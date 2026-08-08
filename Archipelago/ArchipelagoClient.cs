using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using DonutCountyAP.Randomizer;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking.Match;

namespace DonutCountyAP.Archipelago;

public class ArchipelagoClient
{
    record struct ConnectionInfo(string Uri, string SlotName, string Password);

    public const string AP_VERSION = "0.5.0";
    private const string GAME = "Donut County";

    private bool _attemptingConnection;

    //private DeathLinkHandler _deathLinkHandler;
    private ArchipelagoSession _session = null;

    bool _pendingLocationsInFlight = false;
    readonly List<long> _pendingLocations = [];

    ConnectionInfo _thisConnection = new("", "", "");
    ConnectionInfo _lastConnection = new("", "", "");

    public void Connect()
    {
        if (Plugin.GameState != null || _attemptingConnection) return;
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

    private void SetupSession()
    {
        _session.MessageLog.OnMessageReceived += message => ArchipelagoConsole.LogMessage(message.ToString());
        _session.Items.ItemReceived += OnItemReceived;
        _session.Locations.CheckedLocationsUpdated += OnLocationsReceived;
        _session.Socket.ErrorReceived += OnSessionErrorReceived;
        _session.Socket.SocketClosed += OnSessionSocketClosed;
    }

    private void TryConnect()
    {
        try
        {
            _attemptingConnection = true;
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
            Plugin.BepInLogger.LogError(e);
            HandleConnectResult(new LoginFailure(e.ToString()));
            _attemptingConnection = false;
        }
    }

    private void HandleConnectResult(LoginResult result)
    {
        string outText;
        if (result.Successful)
        {
            var success = (LoginSuccessful)result;

            if (_thisConnection != _lastConnection)
            {
                Plugin.BepInLogger.LogInfo("clearning previous locations");
                _pendingLocations.Clear();
                _pendingLocationsInFlight = false;
            }
            Plugin.SetGame(new GameState(_session.DataStorage.GetSlotData<GameOptions>()));
            foreach (var item in _session.Items.AllItemsReceived)
                Plugin.GameState.ReceivedItem((ItemId)item.ItemId);
            foreach (var location in _session.Locations.AllLocationsChecked)
                Plugin.GameState.ReceivedLocation(location);
            Plugin.GameState.Complete = _session.DataStorage.GetClientStatus() == ArchipelagoClientState.ClientGoal;
            // TODO: load achievement state from slot data

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
        _attemptingConnection = false;
    }

    public void Disconnect()
    {
        Plugin.BepInLogger.LogDebug("disconnecting from server...");
        _session?.Socket.Disconnect();
        _session = null;
        Plugin.SetGame(null);
    }

    public void SendMessage(string message)
    {
        _session.Socket.SendPacketAsync(new SayPacket { Text = message });
    }

    private void OnItemReceived(ReceivedItemsHelper helper)
    {
        var receivedItem = helper.DequeueItem();

        Plugin.GameState?.ReceivedItem((ItemId)receivedItem.ItemId);
    }

    private void OnLocationsReceived(ReadOnlyCollection<long> newCheckedLocations)
    {
        if (Plugin.GameState == null)
            return;
        Plugin.BepInLogger.LogDebug($"received {newCheckedLocations.Count} remote locations");
        foreach (var location in newCheckedLocations)
            Plugin.GameState.ReceivedLocation(location);
    }

    public void SendGoal()
    {
        _session.SetGoalAchieved();
    }

    public void SendLocation(long id)
    {
        _pendingLocations.Add(id);
        if (_pendingLocationsInFlight)
            Plugin.BepInLogger.LogDebug("(server is busy)");
    }

    public void FlushPendingLocations()
    {
        if (_pendingLocationsInFlight || _pendingLocations.Count == 0 || _session == null)
            return;
        _pendingLocationsInFlight = true;
        var ids = _pendingLocations.ToArray();
        _pendingLocations.Clear();
        Plugin.BepInLogger.LogDebug($"sending {ids.Length} location(s) to the server");
        _session.Locations.CompleteLocationChecksAsync(response => {
            Plugin.BepInLogger.LogDebug($"server got locations: {response}");
            _pendingLocationsInFlight = false;
        }, ids);
    }

    public bool HasLocation(long id)
    {
        return _session?.Locations.AllLocationsChecked.Contains(id) ?? false;
    }

    private void OnSessionErrorReceived(Exception e, string message)
    {
        Plugin.BepInLogger.LogError(e);
        ArchipelagoConsole.LogMessage(message);
    }

    private void OnSessionSocketClosed(string reason)
    {
        Plugin.BepInLogger.LogError($"Connection to Archipelago lost: {reason}");
        Disconnect();
    }
}