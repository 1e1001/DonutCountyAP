using DonutCountyAP.Generated;
using DonutCountyAP.Patches;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DonutCountyAP.Randomizer;

public partial class GameState
{
    readonly int[] _inventory;
    readonly bool[] _locations;
    readonly int[] _counters;
    // used to prevent camera checks in titlescreen, is there a better way to do that?
    public bool ActiveDelivery = false;

    public GameOptions Options;

    public GameState(GameOptions options) {
        _inventory = new int[(int)ItemId.Length];
        _locations = new bool[Plugin.Logic.LocationsSize];
        _counters = new int[Plugin.Logic.Counters.Length];
        Options = options;
        Options.ApplyPatches();
    }


    Rect _guiRect = new(100, 100, 400, 420);
    const int ROWS_PER_PAGE = 15;
    readonly int _guiPagesItem = ((int)ItemId.Length + ROWS_PER_PAGE - 1) / ROWS_PER_PAGE;
    readonly int _guiPagesLocation = (Plugin.Logic.LocationsSize + ROWS_PER_PAGE) / ROWS_PER_PAGE;
    int _guiPage = 0;
    int _guiPageItem = 1;
    int _guiPageLocation => 1 + _guiPagesItem;
    int _guiPageEnd => 1 + _guiPagesItem + _guiPagesLocation;
    string _guiOptionsText = null;
    KeyValuePair<Transform, string>[] _guiMarkers = null;
    //string _guiDebugKeyText = "";
    //string _guiDebugValueText = "";
    public void OnGUI()
    {
        _guiRect = GUI.Window(0, _guiRect, OnWindowGUI, "Debug");
        if (_guiMarkers != null)
        {
            foreach (var marker in _guiMarkers)
            {
                var camera = RM.camera.GetComponent<Camera>();
                if (marker.Key == null || camera == null)
                    continue;
                var screen = camera.WorldToScreenPoint(marker.Key.position);
                screen.y = Screen.height - screen.y;
                GUI.Label(new Rect(screen.x, screen.y, 1000, 20), marker.Value);
            }
        }
        var evt = Event.current;
        if (evt.type == EventType.KeyDown)
        {
            if (evt.keyCode == KeyCode.F4)
                ReceivedItem(ItemId.DebugToggle);
            if (evt.keyCode == KeyCode.F5)
                _guiMarkers = _guiMarkers == null ? FallStateExtra.DebugMarkers() : null;
        }
    }

    void OnWindowGUI(int _id)
    {
        if (_guiPage < 0)
            _guiPage = 0;
        if (_guiPage > _guiPageEnd)
            _guiPage = _guiPageEnd;
        if (_guiPage == 0)
        {
            GUI.Label(new Rect(10, 20, 380, 20), ActiveDelivery ? "currently delivering" : "not delivering");
            _guiOptionsText ??= JsonConvert.SerializeObject(Options, Formatting.Indented);
            _guiOptionsText = GUI.TextArea(new Rect(10, 40, 380, 330), _guiOptionsText);
            if (GUI.Button(new Rect(10, 375, 185, 20), "refresh"))
                _guiOptionsText = null;
            if (GUI.Button(new Rect(205, 375, 185, 20), "save"))
            {
                try
                {
                    Options = JsonConvert.DeserializeObject<GameOptions>(_guiOptionsText);
                    _guiOptionsText = null;
                    Options.ApplyPatches();
                }
                catch (JsonException e)
                {
                    _guiOptionsText = e.ToString();
                }
            }
            //var debugClient = Plugin.Client as DebugClient;
            //if (debugClient != null)
            //{
            //    foreach (var entry in debugClient.FakeRandomizer)
            //    {
            //        GUILayout.BeginHorizontal();
            //        if (GUILayout.Button("-", GUILayout.Width(20f)))
            //            debugClient.FakeRandomizer.Remove(entry.Key);
            //        GUILayout.Label($"{entry.Key} -> {entry.Value}");
            //        GUILayout.EndHorizontal();
            //    }
            //    GUILayout.BeginHorizontal();
            //    if (GUILayout.Button("+", GUILayout.Width(20f)) && long.TryParse(_guiDebugKeyText, out var location))
            //        debugClient.FakeRandomizer.Add(location, (ItemId)Enum.Parse(typeof(ItemId), _guiDebugValueText));
            //    _guiDebugKeyText = GUILayout.TextField(_guiDebugKeyText, GUILayout.ExpandWidth(false), GUILayout.MinWidth(100));
            //    GUILayout.Label(" -> ", GUILayout.ExpandWidth(false));
            //    _guiDebugValueText = GUILayout.TextField(_guiDebugValueText, GUILayout.ExpandWidth(false), GUILayout.MinWidth(100));
            //    GUILayout.EndHorizontal();
            //}
        }
        else if (_guiPage < _guiPageLocation)
        {
            if (_guiPage == _guiPageItem)
                GUILayout.Label("items");
            int start = (_guiPage - _guiPageItem) * ROWS_PER_PAGE - 1;
            int end = start + ROWS_PER_PAGE;
            if (start < 0)
                start = 0;
            if (end > Plugin.Logic.DebugSortedItems.Length)
                end = Plugin.Logic.DebugSortedItems.Length;
            for (var i = start; i < end; ++i)
            {
                ItemId id = Plugin.Logic.DebugSortedItems[i];
                GUILayout.BeginHorizontal();
                var value = _inventory[(int)id];
                if (GUILayout.Button("-", GUILayout.Width(20f)))
                    _inventory[(int)id] = value - 1;
                if (GUILayout.Button("+", GUILayout.Width(20f)))
                    ReceivedItem(id);
                GUILayout.Label(value.ToString(), GUILayout.Width(25f));
                var has_item = value > 0;
                var will_have_item = GUILayout.Toggle(has_item, id.ToString());
                if (has_item != will_have_item)
                {
                    if (will_have_item)
                        ReceivedItem(id);
                    else
                        _inventory[(int)id] = 0;
                }
                GUILayout.EndHorizontal();
            }
        }
        else
        {
            if (_guiPage == _guiPageLocation)
                GUILayout.Label("locations");
            int start = (_guiPage - _guiPageLocation) * ROWS_PER_PAGE - 1;
            int end = start + ROWS_PER_PAGE;
            if (start < 0)
                start = 0;
            if (end > Plugin.Logic.DebugSortedLocations.Length)
                end = Plugin.Logic.DebugSortedLocations.Length;
            for (var i = start; i < end; ++i)
            {
                var entry = Plugin.Logic.DebugSortedLocations[i];
                var has_location = _locations[entry.Id];
                if (!Options.CanSendLocation(entry.Type))
                    GUI.enabled = false;
                var will_have_location = GUILayout.Toggle(has_location, entry.Name);
                GUI.enabled = true;
                _locations[entry.Id] = will_have_location;
                if (has_location != will_have_location && will_have_location)
                    ReceivedLocation(entry.Id);
            }
        }
        _guiPage = (int)Math.Round(GUI.HorizontalScrollbar(new Rect(10, 400, 380, 20), _guiPage, 1, 0, _guiPageEnd));
        var evt = Event.current;
        if (evt.type == EventType.ScrollWheel)
        {
            if (evt.delta.y > 0)
                ++_guiPage;
            if (evt.delta.y < 0)
                --_guiPage;
        }
        if (evt.type == EventType.KeyDown)
        {
            if (evt.keyCode == KeyCode.PageDown)
                ++_guiPage;
            if (evt.keyCode == KeyCode.PageUp)
                --_guiPage;
        }
        GUI.DragWindow(new Rect(0, 0, 400, 20));
    }

    public bool Has(ItemId id, int atLeast = 1) => _inventory[(int)id] >= atLeast;
    public int Quantity(ItemId id) => _inventory[(int)id];
    public int Counter(int index) => _counters[index];

    public bool HasHole(ItemId id)
    {
        switch (Options.Hole)
        {
            case GameOptions.EffectItemMode.Split:
                return Has(id);
            case GameOptions.EffectItemMode.Global:
                return Has(ItemId.Hole);
            default:
                return true;
        }
    }
    public bool HasCatapult(ItemId id)
    {
        switch (Options.Catapult)
        {
            case GameOptions.EffectItemMode.Split:
                return Has(id);
            case GameOptions.EffectItemMode.Global:
                return Has(ItemId.Catapult);
            default:
                return true;
        }
    }
    public bool HasLocation(int id)
    {
        return _locations[id];
    }

    public void ReceivedItem(ItemId id, bool startOfGame = false)
    {
        // TODO: cleaner way of initializing this
        ++_inventory[(int)id];
        Plugin.BepInLogger.LogDebug($"received item {id}");
        // TODO: any more immediately-occuring updates go here
        if (startOfGame)
            return;
        switch (id)
        {
            // these should exist at all times, so if they somehow don't it's fine to drop the item
            case ItemId.FillerBackflip:
                RM.gameUI?.GetComponent<Backflip>()?.DoBackflip();
                break;
            case ItemId.CementTrap:
                if (HasLocation(Logic.GOAL))
                    break;
                RM.substanceManager?.GetComponent<CementTrap>()?.DoCementTrap();
                break;
            case ItemId.DepthsTrap:
                if (HasLocation(Logic.GOAL))
                    break;
                GlobalPatches.DepthsTrapReroll = true;
                // of note: if on the catapult delivery, this will just reload the catapult
                // the player is about to goal anyways, so it just wastes a little bit of time before then.
                RM.sceneManager.OnQueueLevel("999ft_forced");
                RM.sceneManager.OnPlayQueuedLevel();
                break;
            case ItemId.SnakeDanger:
                var radio = GameObject.FindObjectOfType<RangerRadio>();
                if (radio != null)
                    SnakeDangerPatches.OnDangerEvent(radio);
                break;
            case ItemId.Salt: case ItemId.Pepper:
                var manager = GameObject.FindObjectOfType<SoupManager>();
                if (manager != null)
                    SaltAndPepperPatches.SoupManager_TestSecret(manager);
                break;
            default:
                // other item effects
                FallStateExtra.OnItem(id);
                break;
        }
    }
    public void ReceivedLocation(int id, bool local = false)
    {
        if (_locations[id])
            return;
        if (local)
        {
            if (id == Logic.GOAL)
                Plugin.Client.SendGoal();
            else
                Plugin.Client.SendLocation(id);
        }
        _locations[id] = true;
        Plugin.BepInLogger.LogDebug($"received location {id}");
        // immediately-occuring updates
        if (Plugin.Logic.TrashEvents.TryGetValue(id, out var trackers))
            foreach (var tracker in trackers)
                ++_counters[tracker];
        FallStateExtra.OnLocation(id);
    }
    public void FoundEvent(string name, bool allowInvalid = false)
    {
        if (!Plugin.Logic.Events.TryGetValue(name, out var location))
        {
            if (!allowInvalid)
                Plugin.BepInLogger.LogError($"triggered invalid event {name}");
            return;
        }
        Plugin.BepInLogger.LogDebug($"found event {name}");
        if (!Options.CanSendLocation(location.Type))
            return;
        ReceivedLocation(location.Id, true);
    }

    public bool UnlockedBossfight()
    {
        return Options.GoalArea != GameOptions.GoalAreaMode.Bossfight || DataManager.GetAchievement("WIN_BOSS_FIGHT") > 0;
    }
}
