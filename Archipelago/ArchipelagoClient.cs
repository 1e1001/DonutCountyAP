using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using DonutCountyAP.Generated;
using DonutCountyAP.Randomizer;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;

namespace DonutCountyAP.Archipelago;

public class ArchipelagoClient : IRandomizerClient
{
    record struct ConnectionInfo(string Uri, string SlotName, string Password);

    public const string AP_VERSION_STATUS = $"Archipelago v{VersionInfo.AP_VERSION}";
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
    int _currentItemIndex = 0;
    readonly object _rlLock = new();
    readonly List<long> _receivedLocations = [];

    // TODO: "by default a lot of exceptions in threads/tasks may get lost."
    // add try handlers to more things (in a way that looks nice)
    public ArchipelagoClient()
    {

        _thisConnection = new(Plugin.Options.Uri, Plugin.Options.SlotName, Plugin.Options.Password);

        try
        {
            _session = ArchipelagoSessionFactory.CreateSession(_thisConnection.Uri);
            _session.MessageLog.OnMessageReceived += message => Plugin.BepInLogger.LogMessage(message.ToString());
            _session.Locations.CheckedLocationsUpdated += OnLocationsReceived;
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
                            new Version(VersionInfo.AP_VERSION),
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
    string VersionFormatted(Version version)
    {
        if (version.Revision != -1)
            return $"{version.Major}.{version.Minor}.{version.Build}-preview.{version.Revision}";
        return $"{version.Major}.{version.Minor}.{version.Build}";
    }
    bool TestIncompatibleVersion(string slotVersion) {
        var clientVersion = new Version(VersionInfo.VERSION);
        var compatVersion = new Version(VersionInfo.COMPAT_VERSION);
        var serverVersion = new Version(slotVersion);
        if (serverVersion.Revision != -1 && serverVersion != clientVersion)
        {
            Plugin.BepInLogger.LogWarning($"Server {VersionFormatted(serverVersion)} is unstable and different from client {VersionFormatted(clientVersion)}, issues may occur");
            return false;
        }
        if (serverVersion < compatVersion)
        {
            Plugin.BepInLogger.LogWarning($"Server {VersionFormatted(serverVersion)} is older than minimum supported {VersionFormatted(compatVersion)} of client {VersionFormatted(clientVersion)}, issues may occur");
            return false;
        }
        if (serverVersion.Revision == -1 && clientVersion.Revision != -1 && serverVersion >= new Version(clientVersion.Major, clientVersion.Minor, clientVersion.Build))
        {
            Plugin.BepInLogger.LogWarning($"Server {VersionFormatted(serverVersion)} is newer than client {VersionFormatted(clientVersion)}, please update your client");
            return true;
        }
        if (serverVersion > clientVersion)
        {
            Plugin.BepInLogger.LogWarning($"Server {VersionFormatted(serverVersion)} is newer than client {VersionFormatted(clientVersion)}, please update your client");
            return true;
        }
        return false;
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
            if (TestIncompatibleVersion(slotData.Version))
            {
                Disconnect();
                return;
            }
            if (_session.DataStorage.GetClientStatus() == ArchipelagoClientState.ClientGoal)
                Plugin.GameState.ReceivedLocation(Logic.GOAL);
            var cacheId = $"{_session.RoomState.Seed}:{_session.ConnectionInfo.Slot}";
            lock (Plugin.Options.CacheLock)
            {
                if (Plugin.Options.CacheId == cacheId)
                {
                    // no need to lock/notify as no queue thread is running
                    _queuedLocations.AddRange(Plugin.Options.CacheLocations);
                } else
                {
                    Plugin.Options.CacheLocations.Clear();
                    Plugin.Options.CacheId = cacheId;
                    Plugin.Options.CacheReceived = _session.Items.AllItemsReceived.Count();
                }
            }
            DataManager.SaveGameData();

            _thread = new(PacketQueueThread);
            _thread.Start();

            //_deathLinkHandler = new(_session.CreateDeathLinkService(), ServerData.SlotName);
            outText = $"Successfully connected to {_thisConnection.Uri} as {_thisConnection.SlotName}!";

            Plugin.BepInLogger.LogMessage(outText);
        }
        else
        {
            var failure = (LoginFailure)result;
            outText = $"Failed to connect to {Plugin.Options.Uri} as {Plugin.Options.SlotName}.";
            outText = failure.Errors.Aggregate(outText, (current, error) => current + $"\n    {error}");

            Plugin.BepInLogger.LogError(outText);

            Disconnect();
        }
    }
    void OnLocationsReceived(ReadOnlyCollection<long> newCheckedLocations)
    {
        lock (_rlLock)
            _receivedLocations.AddRange(newCheckedLocations);
    }
    void OnSessionErrorReceived(Exception e, string message)
    {
        Plugin.BepInLogger.LogError(e);
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

            Plugin.BepInLogger.LogDebug($"sending {packets.Count} packets");
            // always run an extra iteration because i'm not confident in my multithreading
            if (packets.Count > 0)
            {
                _session.Socket.SendMultiplePackets(packets);
                Plugin.BepInLogger.LogDebug($"sent packets!");
                System.Threading.Thread.Sleep(500);
            }
            else
            {
                _wait.WaitOne();
            }
        }
    }

    // impl IRandomizerClient
    public bool Connecting() => Plugin.GameState == null;
    public void Update()
    {
        if (Plugin.GameState == null)
            return;
        while (_session.Items.Any())
        {
            var item = _session.Items.DequeueItem();
            Plugin.GameState.ReceivedItem((ItemId)item.ItemId, Plugin.Options.CacheReceived > _currentItemIndex);
            if (++_currentItemIndex > Plugin.Options.CacheReceived)
                Plugin.Options.CacheReceived = _currentItemIndex;
        }
        // this is a kinda huge critical section but it's fine for the socket thread to just wait it out
        lock (_rlLock)
        {
            if (_receivedLocations.Count > 0)
            {
                lock (Plugin.Options.CacheLock)
                    foreach (var location in _receivedLocations)
                        if (Plugin.Options.CacheLocations.Contains(location))
                            Plugin.Options.CacheLocations.Remove(location);
                foreach (var location in _receivedLocations)
                    Plugin.GameState.ReceivedLocation((int)location);
                _receivedLocations.Clear();
            }
        }
    }
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
        lock (_lock)
            _queuedLocations.Add(id);
        lock (Plugin.Options.CacheLock)
            if (!Plugin.Options.CacheLocations.Contains(id))
                Plugin.Options.CacheLocations.Add(id);
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
