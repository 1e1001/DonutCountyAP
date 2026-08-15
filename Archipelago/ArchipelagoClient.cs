using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using DonutCountyAP.Randomizer;

namespace DonutCountyAP.Archipelago;

public class ArchipelagoClient : IRandomizerClient
{
    record struct ConnectionInfo(string Uri, string SlotName, string Password);

    public const string AP_VERSION = "0.5.0";
    public const string AP_VERSION_STATUS = $"Archipelago v{AP_VERSION}";
    public const string AP_DEFAULT_STATUS = $"{AP_VERSION_STATUS} Disconnected";
    const string GAME = "Donut County";

    //private DeathLinkHandler _deathLinkHandler;
    ArchipelagoSession _session = null;

    ConnectionInfo _thisConnection = new("", "", "");

    // these are static as they need to persist between client connections
    static readonly List<long> _pendingLocations = [];
    static ConnectionInfo _lastConnection = new("", "", "");

    public ArchipelagoClient()
    {
        _lastConnection = _thisConnection;
        _thisConnection = new(Plugin.RandomizerData.Uri, Plugin.RandomizerData.SlotName, Plugin.RandomizerData.Password);

        try
        {
            _session = ArchipelagoSessionFactory.CreateSession(_thisConnection.Uri);
            SetupSession();
        }
        catch (Exception e)
        {
            Plugin.BepInLogger.LogError(e);
        }

        TryConnect();
    }

    void SetupSession()
    {
        _session.MessageLog.OnMessageReceived += message => ArchipelagoConsole.LogMessage(message.ToString());
        _session.Items.ItemReceived += OnItemReceived;
        _session.Locations.CheckedLocationsUpdated += OnLocationsReceived;
        _session.Socket.ErrorReceived += OnSessionErrorReceived;
        _session.Socket.SocketClosed += OnSessionSocketClosed;
    }

    void TryConnect()
    {
        try
        {
            // it's safe to thread this function call but unity notoriously hates threading so do not use excessively
            ThreadPool.QueueUserWorkItem(
                _ => HandleConnectResult(
                    _session.TryConnectAndLogin(
                        GAME,
                        _thisConnection.SlotName,
                        ItemsHandlingFlags.AllItems,
                        new Version(AP_VERSION),
                        password: _thisConnection.Password,
                        requestSlotData: true
                    )));
        }
        catch (Exception e)
        {
            Plugin.BepInLogger.LogError($"bad: {e}");
            HandleConnectResult(new LoginFailure(e.ToString()));
        }
    }

    void SlotDataKey(string key, Action<int> callback)
    {
        _session.DataStorage[Scope.Slot, key].Initialize(0);
        _session.DataStorage[Scope.Slot, key].GetAsync(callback);
    }
    void LoadSlotData()
    {
        SlotDataKey("has_seen_gameover_cutscene", data => DataManager.gameData.hasSeenGameOverCutscene = data);
        SlotDataKey("RELEASE_HOT_AIR_BALLOON", data => DataManager.gameData.RELEASE_HOT_AIR_BALLOON = data);
        SlotDataKey("DESTROY_DONUT_SHOP", data => DataManager.gameData.DESTROY_DONUT_SHOP = data);
        SlotDataKey("DESTROY_RACCOON_LAGOON", data => DataManager.gameData.DESTROY_RACCOON_LAGOON = data);
        SlotDataKey("HACK_HQ", data => DataManager.gameData.HACK_HQ = data);
        SlotDataKey("WIN_BOSS_FIGHT", data => DataManager.gameData.WIN_BOSS_FIGHT = data);
        SlotDataKey("CATAPULT_COMPLETE", data => DataManager.gameData.CATAPULT_COMPLETE = data);
        SlotDataKey("COMPLETE_TRASHOPEDIA", data => DataManager.gameData.COMPLETE_TRASHOPEDIA = data);
        SlotDataKey("BUY_GAMER_FUEL", data => DataManager.gameData.BUY_GAMER_FUEL = data);
        SlotDataKey("SET_TRAILER_ON_FIRE", data => DataManager.gameData.SET_TRAILER_ON_FIRE = data);
        SlotDataKey("QUACK_100_TIMES", data => DataManager.gameData.QUACK_100_TIMES = data);
        SlotDataKey("BREAK_EGGS", data => DataManager.gameData.BREAK_EGGS = data);
        SlotDataKey("COLLECT_RADIO_LAST", data => DataManager.gameData.COLLECT_RADIO_LAST = data);
        SlotDataKey("FLAWLESS_BOSS_FIGHT", data => DataManager.gameData.FLAWLESS_BOSS_FIGHT = data);
        SlotDataKey("LOSE_BOSS_FIGHT", data => DataManager.gameData.LOSE_BOSS_FIGHT = data);
        SlotDataKey("DESTROY_MONITOR", data => DataManager.gameData.DESTROY_MONITOR = data);
        SlotDataKey("DESTROY_MONUMENT", data => DataManager.gameData.DESTROY_MONUMENT = data);
        SlotDataKey("MAKE_SECRET_SOUP", data => DataManager.gameData.MAKE_SECRET_SOUP = data);
        SlotDataKey("FLY_THROUGH_DONUT_HOLE", data => DataManager.gameData.FLY_THROUGH_DONUT_HOLE = data);
        SlotDataKey("FIND_AIRSHIP", data => DataManager.gameData.FIND_AIRSHIP = data);
        SlotDataKey("UNLOCK_HQ_VAULT", data => DataManager.gameData.UNLOCK_HQ_VAULT = data);
    }

    void HandleConnectResult(LoginResult result)
    {
        string outText;
        if (result.Successful)
        {
            var success = (LoginSuccessful)result;

            if (_thisConnection != _lastConnection)
            {
                Plugin.BepInLogger.LogInfo("clearning previous locations");
                _pendingLocations.Clear();
            }
            LoadSlotData();
            Plugin.SetGame(new GameState(_session.DataStorage.GetSlotData<GameOptions>()));
            foreach (var item in _session.Items.AllItemsReceived)
                Plugin.GameState.ReceivedItem((ItemId)item.ItemId, true);
            foreach (var location in _session.Locations.AllLocationsChecked)
                Plugin.GameState.ReceivedLocation(location);

            //_deathLinkHandler = new(_session.CreateDeathLinkService(), ServerData.SlotName);
            outText = $"Successfully connected to {Plugin.RandomizerData.Uri} as {Plugin.RandomizerData.SlotName}!";

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

    public void SendMessage(string message)
    {
        _session.Socket.SendPacketAsync(new SayPacket { Text = message });
    }

    void OnItemReceived(ReceivedItemsHelper helper)
    {
        var receivedItem = helper.DequeueItem();

        Plugin.GameState?.ReceivedItem((ItemId)receivedItem.ItemId);
    }

    void OnLocationsReceived(ReadOnlyCollection<long> newCheckedLocations)
    {
        if (Plugin.GameState == null)
            return;
        Plugin.BepInLogger.LogDebug($"received {newCheckedLocations.Count} remote locations");
        foreach (var location in newCheckedLocations)
            Plugin.GameState.ReceivedLocation(location);
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

    // impl IRandomizerClient
    public void Update()
    {
        // TODO: learn more about how to synchronize with threadpool and make this properly threaded
        if (_pendingLocations.Count == 0)
            return;
        Plugin.BepInLogger.LogDebug($"sending {_pendingLocations.Count} location(s) to the server");
        _session.Locations.CompleteLocationChecks([.. _pendingLocations]);
        _pendingLocations.Clear();
        Plugin.BepInLogger.LogDebug($"sent!");
    }
    public string GUIStatus()
    {
        return Plugin.GameState != null ? $"{AP_VERSION_STATUS} Connected" : $"{AP_VERSION_STATUS} Connecting...";
    }
    public bool IsComplete()
    {
        // this has a tendency to fail for some reason?
        try {
            return Plugin.GameState != null && _session.DataStorage.GetClientStatus() == ArchipelagoClientState.ClientGoal;
        } catch (Exception e)
        {
            Plugin.BepInLogger.LogWarning($"Failed to get goal status: {e}");
            return false;
        }
    }
    public void SendGoal()
    {
        _session.SetGoalAchieved();
    }
    public void SendLocation(long id)
    {
        _pendingLocations.Add(id);
    }
    public void Disconnect()
    {
        Plugin.BepInLogger.LogDebug("disconnecting from server...");
        _session.Socket.Disconnect();
        Plugin.Client = null;
        Plugin.SetGame(null);
    }
    public ICollection<long> Locations()
    {
        return _session.Locations.AllLocationsChecked;
    }
    public void SetSlotData(string key, int value)
    {
        _session.DataStorage[Scope.Slot, key] = value;
    }
    public void SetSlotDataMax(string key, int value)
    {
        _session.DataStorage[Scope.Slot, key] += Operation.Max(value);
    }
}