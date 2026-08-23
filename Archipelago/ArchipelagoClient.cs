using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using DonutCountyAP.Randomizer;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace DonutCountyAP.Archipelago;

public class ArchipelagoClient : IRandomizerClient
{
    record struct ConnectionInfo(string Uri, string SlotName, string Password);

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
    readonly Dictionary<string, JToken> _queuedDataStorage = [];
    bool _queuedGoal = false;

    // TODO: "by default a lot of exceptions in threads/tasks may get lost."
    // add try handlers to more things (in a way that looks nice)
    public ArchipelagoClient()
    {

        _thisConnection = new(Plugin.RandomizerData.Uri, Plugin.RandomizerData.SlotName, Plugin.RandomizerData.Password);

        try
        {
            _session = ArchipelagoSessionFactory.CreateSession(_thisConnection.Uri);
            _session.MessageLog.OnMessageReceived += message => ArchipelagoConsole.LogMessage(message.ToString());
            _session.Socket.ErrorReceived += OnSessionErrorReceived;
            _session.Socket.SocketClosed += OnSessionSocketClosed;
            Plugin.BepInLogger.LogDebug("doing connect");
            // it's safe to thread this function call but unity notoriously hates threading so do not use excessively
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    HandleConnectResult(
                        _session.TryConnectAndLogin(
                            GAME,
                            _thisConnection.SlotName,
                            ItemsHandlingFlags.AllItems,
                            new Version(AP_VERSION),
                            password: _thisConnection.Password,
                            requestSlotData: false
                        )
                    );
                }
                catch (Exception e)
                {
                    HandleConnectResult(new LoginFailure(e.ToString()));
                    return;
                }
            });
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
            var slotData = _session.DataStorage.GetSlotData<GameOptions>();
            Plugin.SetGame(new GameState(slotData));
            if (Plugin.GameState.Options.Version != Plugin.PLUGIN_VERSION)
                ArchipelagoConsole.LogMessage($"World version {Plugin.GameState.Options.Version} is different from client version {Plugin.PLUGIN_VERSION}, issues may occur!");
            if (_session.DataStorage.GetClientStatus() == ArchipelagoClientState.ClientGoal)
                Plugin.GameState.ReceivedLocation(AutoLogic.LOCATION_GOAL);
            _session.Items.ItemReceived += OnItemReceived;
            _session.Locations.CheckedLocationsUpdated += OnLocationsReceived;
            foreach (var item in _session.Items.AllItemsReceived)
                Plugin.GameState.ReceivedItem((ItemId)item.ItemId, true);
            foreach (var location in _session.Locations.AllLocationsChecked)
                Plugin.GameState.ReceivedLocation((int)location);
            var cacheId = $"{_session.RoomState.Seed}:{_session.ConnectionInfo.Slot}";
            if (Plugin.RandomizerData.LocationCacheId == cacheId)
            {
                // no need to lock as thread isn't running yet
                _queuedLocations.AddRange(Plugin.RandomizerData.LocationCache);
            } else
            {
                Plugin.RandomizerData.LocationCache.Clear();
                Plugin.RandomizerData.LocationCacheId = cacheId;
            }
            DataManager.SaveGameData();

            _thread = new(PacketQueueThread);
            _thread.Start();

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
        Plugin.BepInLogger.LogDebug($"received {newCheckedLocations.Count} remote locations");
        foreach (var location in newCheckedLocations)
        {
            if (Plugin.RandomizerData.LocationCache.Contains(location))
                Plugin.RandomizerData.LocationCache.Remove(location);
            Plugin.GameState.ReceivedLocation((int)location);
        }
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
            KeyValuePair<string, JToken>[] queuedDataStorage;
            bool queuedGoal;
            lock (_lock)
            {
                queuedLocations = [.. _queuedLocations];
                _queuedLocations.Clear();
                queuedChat = [.. _queuedChat];
                _queuedChat.Clear();
                queuedDataStorage = [.. _queuedDataStorage];
                _queuedDataStorage.Clear();
                queuedGoal = _queuedGoal;
                _queuedGoal = false;
            }
            if (queuedLocations.Length > 0)
                packets.Add(new LocationChecksPacket() { Locations = queuedLocations });
            foreach (var text in queuedChat)
                packets.Add(new SayPacket() { Text = text });
            foreach (var kv in queuedDataStorage)
            {
                packets.Add(new SetPacket()
                {
                    Key = $"Slot:{_session.ConnectionInfo.Slot}:{kv.Key}",
                    Operations = [new OperationSpecification()
                    {
                        OperationType = OperationType.Replace,
                        Value = kv.Value,
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
    public bool Connecting() => Plugin.GameState == null;
    public string GUIStatus()
    {
        return AP_VERSION_STATUS;
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
        //lock (_lock)
        //    _queuedLocations.Add(id);
        if (!Plugin.RandomizerData.LocationCache.Contains(id))
           Plugin.RandomizerData.LocationCache.Add(id);
        // TODO: queue save of randomizer data? how often does it save mid-game
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
    public void SetSlotStorage(string key, JToken value) {
        lock (_lock)
            if (!_queuedDataStorage.ContainsKey(key))
                _queuedDataStorage.Add(key, value);
        _wait.Set();
    }
}
