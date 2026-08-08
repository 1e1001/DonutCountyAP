using DonutCountyAP.Patches;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DonutCountyAP.Randomizer;

public partial class GameState
{
    Dictionary<ItemId, int> _inventory = [];
    // TODO: debug tracker should probably be implemented as a different type of "archipelago client" instead
    HashSet<long> _debugTrackerLocations = [];
    public bool Complete = false;
    public bool ActiveDelivery = false;

    public GameOptions Options;

    public GameState(GameOptions options) {
        Options = options;
        Options.ApplyPatches();
    }


    private Rect _guiRect = new(100, 100, 400, 400);
    private Vector2 _guiScroll;
    private string _guiOptionsText = null;
    public void OnGUI()
    {
        _guiRect = GUI.Window(0, _guiRect, OnWindowGUI, "Debug");
    }

    void OnWindowGUI(int _id)
    {
        _guiScroll = GUILayout.BeginScrollView(_guiScroll, false, true);
        GUILayout.Label(ActiveDelivery ? "currently delivering" : "not delivering");
        _guiOptionsText ??= JsonConvert.SerializeObject(Options, Formatting.Indented);
        _guiOptionsText = GUILayout.TextArea(_guiOptionsText);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("refresh"))
            _guiOptionsText = null;
        if (GUILayout.Button("save"))
        {
            try
            {
                Options = JsonConvert.DeserializeObject<GameOptions>(_guiOptionsText);
                _guiOptionsText = null;
                Options.ApplyPatches();
            } catch (JsonException e)
            {
                _guiOptionsText = e.ToString();
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        var patchEnabled = Plugin.Patcher.DebugFallState.Enabled;
        Plugin.Patcher.DebugFallState.Set(GUILayout.Toggle(patchEnabled, "fallstate"));
        GUILayout.EndHorizontal();
        if (patchEnabled)
        {
            GUILayout.TextArea(JsonConvert.SerializeObject(DebugFallStatePatches.ObjectList, Formatting.Indented));
        }
        GUILayout.Label("items");
        foreach (ItemId i in Enum.GetValues(typeof(ItemId)))
        {
            GUILayout.BeginHorizontal();
            _inventory.TryGetValue(i, out int value);
            if (GUILayout.Button("-", GUILayout.Width(20f)))
                _inventory[i] = value - 1;
            if (GUILayout.Button("+", GUILayout.Width(20f)))
                ReceivedItem(i);
            var has_item = value > 0;
            var will_have_item = GUILayout.Toggle(has_item, "", GUILayout.Width(15f));
            if (has_item != will_have_item)
            {
                if (will_have_item)
                    ReceivedItem(i);
                else if (value == 1)
                    _inventory.Remove(i);
            }
            GUILayout.Label(value.ToString(), GUILayout.Width(25f));
            GUILayout.Label(i.ToString(), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
        GUILayout.Label("locations");
        foreach (AutoLogic.DebugTracker entry in AutoLogic.DEBUG_TRACKER)
        {
            GUILayout.BeginHorizontal();
            var has_location = _debugTrackerLocations.Contains(entry.Location.Id);
            var will_have_location = GUILayout.Toggle(has_location, "", GUILayout.Width(15f));
            if (has_location != will_have_location)
            {
                if (will_have_location)
                    ReceivedLocation(entry.Location.Id);
                else
                    _debugTrackerLocations.Remove(entry.Location.Id);
            }
            var oldColor = GUI.contentColor;
            if (!Options.CanSendLocation(entry.Location.Type))
                GUI.contentColor = Color.grey;
            GUILayout.Label(entry.Name, GUILayout.ExpandWidth(false));
            GUI.contentColor = oldColor;
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    public bool Has(ItemId id, int atLeast = 1)
    {
        return _inventory.TryGetValue(id, out var value) && value >= atLeast;
    }
    public int Quantity(ItemId id)
    {
        return _inventory.TryGetValue(id, out var value) ? value : 0;
    }

    public bool HasCatapult(ItemId id)
    {
        switch (Options.Catapult)
        {
            case GameOptions.CatapultMode.Split:
                return Has(id);
            case GameOptions.CatapultMode.Global:
                return Has(ItemId.Catapult);
            default:
                return true;
        }
    }

    public void ReceivedItem(ItemId id, bool startOfGame = false)
    {
        // TODO: cleaner way of this
        if (!_inventory.ContainsKey(id))
            _inventory[id] = 0;
        ++_inventory[id];
        Plugin.BepInLogger.LogDebug($"received item {id}");
        // TODO: any more immediately-occuring updates go here
        if (startOfGame)
            return;
        switch(id)
        {
            // these should exist at all times, so if they somehow don't it's fine to drop the item
            case ItemId.FillerBackflip:
                RM.gameUI?.GetComponent<Backflip>()?.DoBackflip();
                break;
            case ItemId.CementTrap:
                RM.substanceManager?.GetComponent<CementTrap>()?.DoCementTrap();
                break;
            case ItemId.DepthsTrap:
                GlobalPatches.DepthsTrapReroll = true;
                // don't use OnQueueLevel so it doesn't get caught by LevelTransitionPatches
                RM.sceneManager.nextLevel = "999ft";
                RM.sceneManager.OnPlayQueuedLevel();
                break;
        }
    }
    public void ReceivedLocation(long id)
    {
        _debugTrackerLocations.Add(id);
        Plugin.BepInLogger.LogDebug($"received location {id}");
        // TODO: update trackers
    }
    public void FoundEvent(string name, bool allowInvalid = false)
    {
        if (!AutoLogic.EVENTS.TryGetValue(name, out var location))
        {
            if (!allowInvalid)
                Plugin.BepInLogger.LogWarning($"triggered invalid event {name}");
            return;
        }
        Plugin.BepInLogger.LogInfo($"found event {name}");
        if (!Options.CanSendLocation(location.Type))
            return;
        if (location.Type == AutoLogic.LocationType.Victory)
        {
            Complete = true;
            Plugin.ArchipelagoClient.SendGoal();
            return;
        }
        if (!Plugin.ArchipelagoClient.HasLocation(location.Id))
            Plugin.ArchipelagoClient.SendLocation(location.Id);
        ReceivedLocation(location.Id);
    }

    public bool HasLocation(long id)
    {
        return _debugTrackerLocations.Contains(id);
        //return Plugin.ArchipelagoClient.HasLocation(id);
    }

    public bool UnlockedBossfight()
    {
        return Options.GoalArea != GameOptions.GoalAreaMode.Bossfight || DataManager.GetAchievement("WIN_BOSS_FIGHT") > 0;
    }
}
