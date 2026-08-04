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
using DonutCountyAP.Utils;
using Newtonsoft.Json.Linq;

namespace DonutCountyAP.Archipelago;

public class ArchipelagoClient
{
    // TODO: de-static this implementation
    public const string AP_VERSION = "0.5.0";
    private const string GAME = "Donut County";

    private bool _attemptingConnection;

    //private DeathLinkHandler _deathLinkHandler;
    private ArchipelagoSession _session = null;

    public void Connect()
    {
        if (Plugin.GameState != null || _attemptingConnection) return;

        try
        {
            _session = ArchipelagoSessionFactory.CreateSession(Plugin.RandomizerData.Uri);
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
                        Plugin.RandomizerData.SlotName,
                        ItemsHandlingFlags.AllItems,
                        new Version(AP_VERSION),
                        password: Plugin.RandomizerData.Password,
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

            Plugin.SetGame(new GameState(_session.DataStorage.GetSlotData<GameOptions>(), from loc in _session.Locations.AllLocations select (CheckId)loc));

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

        Plugin.GameState?.ReceivedItem((CheckId)receivedItem.ItemId);
    }

    private void OnLocationsReceived(ReadOnlyCollection<long> newCheckedLocations)
    {
        foreach (var location in newCheckedLocations)
            Plugin.GameState?.ReceivedLocation((CheckId)location);
    }

    bool _pendingLocationsInFlight = false;
    readonly List<long> _pendingLocations = [];

    public void SendLocation(CheckId id)
    {
        _pendingLocations.Add((long)id);
        if (!_pendingLocationsInFlight)
            FlushPendingLocations();
    }

    void FlushPendingLocations()
    {
        _pendingLocationsInFlight = true;
        var ids = _pendingLocations.ToArray();
        _pendingLocations.Clear();
        Plugin.BepInLogger.LogDebug($"sending {ids.Length} location(s) to the server");
        // TODO: this batching does not work
        _session?.Locations.CompleteLocationChecksAsync(_ => {
            Plugin.BepInLogger.LogDebug("got server response");
            if (_pendingLocations.Count > 0)
                FlushPendingLocations();
            else
                _pendingLocationsInFlight = false;
        }, ids);
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