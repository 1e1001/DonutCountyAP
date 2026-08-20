using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using DonutCountyAP.Randomizer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace DonutCountyAP.Archipelago;

public class ArchipelagoClient : IRandomizerClient
{
    record struct ConnectionInfo(string Uri, string SlotName, string Password);
    record struct QueuedSlotData(string Key, int Value);

    public const string AP_VERSION = "0.5.0";
    public const string AP_VERSION_STATUS = $"Archipelago v{AP_VERSION}";
    public const string AP_DEFAULT_STATUS = $"{AP_VERSION_STATUS} Disconnected";
    const string GAME = "Donut County";

    //private DeathLinkHandler _deathLinkHandler;
    readonly ArchipelagoSession _session = null;

    ConnectionInfo _thisConnection = new("", "", "");

    readonly object _lock = new();
    Thread _thread = null;
    readonly EventWaitHandle _wait = new(false, EventResetMode.AutoReset);
    readonly List<long> _queuedLocations = [];
    readonly List<string> _queuedChat = [];
    int _queuedDSLevelSelect = -1;
    bool _queuedGoal = false;

    public ArchipelagoClient()
    {

        _thisConnection = new(Plugin.RandomizerData.Uri, Plugin.RandomizerData.SlotName, Plugin.RandomizerData.Password);

        try
        {
            _session = ArchipelagoSessionFactory.CreateSession(_thisConnection.Uri);
            _session.MessageLog.OnMessageReceived += message => ArchipelagoConsole.LogMessage(message.ToString());
            _session.Items.ItemReceived += OnItemReceived;
            _session.Locations.CheckedLocationsUpdated += OnLocationsReceived;
            _session.Socket.ErrorReceived += OnSessionErrorReceived;
            _session.Socket.SocketClosed += OnSessionSocketClosed;
            Plugin.BepInLogger.LogDebug("doing connect");
            // it's safe to thread this function call but unity notoriously hates threading so do not use excessively
            ThreadPool.QueueUserWorkItem(
                _ => HandleConnectResult(
                    _session.TryConnectAndLogin(
                        GAME,
                        _thisConnection.SlotName,
                        ItemsHandlingFlags.AllItems,
                        new Version(AP_VERSION),
                        password: _thisConnection.Password,
                        requestSlotData: false
                    )
                )
            );
        }
        catch (Exception e)
        {
            HandleConnectResult(new LoginFailure(e.ToString()));
        }

    }
    void HandleConnectResult(LoginResult result)
    {
        Plugin.BepInLogger.LogDebug($"connect result {result.Successful}");
        string outText;
        if (result.Successful)
        {
            var success = (LoginSuccessful)result;
            try
            {
                var slotData = _session.DataStorage.GetSlotData<GameOptions>();
                Plugin.SetGame(new GameState(slotData));
                if (Plugin.GameState.Options.Version != Plugin.PLUGIN_VERSION)
                    Plugin.BepInLogger.LogWarning($"World version {Plugin.GameState.Options.Version} is different from client version {Plugin.PLUGIN_VERSION}, issues may occur!");
                foreach (var item in _session.Items.AllItemsReceived)
                    Plugin.GameState.ReceivedItem((ItemId)item.ItemId, true);
                foreach (var location in _session.Locations.AllLocationsChecked)
                    Plugin.GameState.ReceivedLocation((int)location);
                if (_session.DataStorage.GetClientStatus() == ArchipelagoClientState.ClientGoal)
                    Plugin.GameState.ReceivedLocation(AutoLogic.LOCATION_GOAL);

                _thread = new(PacketQueueThread);
                _thread.Start();
            }
            catch (Exception e)
            {
                HandleConnectResult(new LoginFailure(e.ToString()));
                return;
            }

            //_deathLinkHandler = new(_session.CreateDeathLinkService(), ServerData.SlotName);
            outText = $"Successfully connected to {_thisConnection.Uri} as {_thisConnection.SlotName}!";

            ArchipelagoConsole.LogMessage(outText);
        }
        else
        {
            var failure = (LoginFailure)result;
            outText = $"Failed to connect to {Plugin.RandomizerData.Uri} as {Plugin.RandomizerData.SlotName}.";
            outText = failure.Errors.Aggregate(outText, (current, error) => current + $"\n    {error}");

            Plugin.BepInLogger.LogError(outText);

            Disconnect();
        }

        ArchipelagoConsole.LogMessage(outText);
    }

    void OnItemReceived(ReceivedItemsHelper helper)
    {
        var receivedItem = helper.DequeueItem();

        Plugin.GameState.ReceivedItem((ItemId)receivedItem.ItemId);
    }

    void OnLocationsReceived(ReadOnlyCollection<long> newCheckedLocations)
    {
        if (Plugin.GameState == null)
            return;
        Plugin.BepInLogger.LogDebug($"received {newCheckedLocations.Count} remote locations");
        foreach (var location in newCheckedLocations)
            Plugin.GameState.ReceivedLocation((int)location);
    }
    void OnSessionErrorReceived(Exception e, string message)
    {
        Plugin.BepInLogger.LogError(e);
        ArchipelagoConsole.LogMessage(message);
    }

    void OnSessionSocketClosed(string reason)
    {
        Plugin.BepInLogger.LogError($"Connection to Archipelago lost: {reason}");
        Disconnect();
    }

    void PacketQueueThread()
    {
        while (true)
        {
            var packets = new List<ArchipelagoPacketBase>();
            long[] queuedLocations;
            string[] queuedChat;
            int queuedDSLevelSelect;
            bool queuedGoal;
            lock (_lock)
            {
                queuedLocations = [.. _queuedLocations];
                _queuedLocations.Clear();
                queuedChat = [.. _queuedChat];
                _queuedChat.Clear();
                queuedDSLevelSelect = _queuedDSLevelSelect;
                _queuedDSLevelSelect = -1;
                queuedGoal = _queuedGoal;
                _queuedGoal = false;
            }
            if (queuedLocations.Length > 0)
                packets.Add(new LocationChecksPacket() { Locations = queuedLocations });
            foreach (var text in queuedChat)
                packets.Add(new SayPacket() { Text = text });
            if (queuedDSLevelSelect != -1)
            {
                packets.Add(new SetPacket()
                {
                    Key = $"Slot:{_session.ConnectionInfo.Slot}:level",
                    Operations = [new OperationSpecification()
                    {
                        OperationType = OperationType.Replace,
                        Value = queuedDSLevelSelect,
                    }],
                });
            }
            if (queuedGoal)
                packets.Add(new StatusUpdatePacket() { Status = ArchipelagoClientState.ClientGoal });

            // always run an extra iteration because i'm not confident in my multithreading
            if (packets.Count > 0)
            {
                Plugin.BepInLogger.LogDebug($"sending {packets.Count} packets");
                _session.Socket.SendMultiplePackets(packets);
                Plugin.BepInLogger.LogDebug($"sent packets!");
            }
            else
            {
                _wait.WaitOne();
            }
        }
    }

    // impl IRandomizerClient
    public void Update() { }
    public string GUIStatus()
    {
        return Plugin.GameState != null ? $"{AP_VERSION_STATUS} Connected" : $"{AP_VERSION_STATUS} Connecting...";
    }
    public void SendChat(string text) {
        lock (_lock)
            _queuedChat.Add(text);
        _wait.Set();
    }
    public void SendGoal() {
        lock (_lock)
            _queuedGoal = true;
        _wait.Set();
    }
    public void SendLocation(long id)
    {
        lock (_lock)
            _queuedLocations.Add(id);
        _wait.Set();
    }
    public void Disconnect()
    {
        Plugin.BepInLogger.LogDebug("disconnecting from server...");
        _session?.Socket.Disconnect();
        // deprecated but i don't care it still works
        _thread?.Abort();
        Plugin.SetGame(null);
    }
    public void SetDSLevelSelect(int value) {
        lock (_lock)
            _queuedDSLevelSelect = value;
        _wait.Set();
    }
    //public void SetSlotDataMax(string _key, int _value) { }
}
