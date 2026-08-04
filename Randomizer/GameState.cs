using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DonutCountyAP.Randomizer;

public partial class GameState
{
    Dictionary<CheckId, int> _items = [];
    HashSet<CheckId> _locations = [];
    HashSet<CheckId> _allLocations = [];
    public bool ActiveDelivery = false;

    public GameOptions Options;

    public GameState(GameOptions options, IEnumerable<CheckId> allLocations = null) {
        _allLocations = new HashSet<CheckId>(allLocations ?? (IEnumerable<CheckId>)Enum.GetValues(typeof(CheckId)));
        _allLocations.Remove(CheckId.None);
        Options = options;
        GrantDisabledItems(options.Water, [CheckId.HoleWater]);
        GrantDisabledItems(options.Fire, [CheckId.HoleFire]);
        GrantDisabledItems(options.Snake, [CheckId.HoleSnake]);
        GrantDisabledItems(options.Light, [CheckId.HoleLight]);
        GrantDisabledItems(options.Bunnies, [CheckId.HoleBunnies]);
        GrantDisabledItems(options.Catapult == GameOptions.CatapultMode.Global, [CheckId.Catapult]);
        GrantDisabledItems(options.Catapult == GameOptions.CatapultMode.Individual, [
            CheckId.CatapultBoxes,
            CheckId.CatapultChickens,
            CheckId.CatapultEggs,
            CheckId.CatapultHoneycomb,
            CheckId.CatapultFrogs,
            CheckId.CatapultWaterBalloons,
            CheckId.CatapultWater,
            CheckId.CatapultDonutsCamerasRaccoons,
            CheckId.CatapultHackingDevice,
            CheckId.CatapultBombs,
        ]);
    }

    void GrantDisabledItems(bool enabled, CheckId[] items)
    {
        if (!enabled)
            foreach (var item in items)
                ReceivedItem(item);
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
        GUILayout.Label(ActiveDelivery ? "Currently delivering" : "Not delivering");
        _guiOptionsText ??= JsonConvert.SerializeObject(Options, Formatting.Indented);
        _guiOptionsText = GUILayout.TextArea(_guiOptionsText);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh"))
            _guiOptionsText = null;
        if (GUILayout.Button("Save"))
        {
            try
            {
                Options = JsonConvert.DeserializeObject<GameOptions>(_guiOptionsText);
                _guiOptionsText = null;
            } catch (JsonException e)
            {
                _guiOptionsText = e.ToString();
            }
        }
        if (GUILayout.Button("Reset items"))
        {
            Plugin.SetGame(new GameState(Options, []) { _allLocations = _allLocations });
        }
        GUILayout.EndHorizontal();
        foreach (CheckId i in Enum.GetValues(typeof(CheckId)))
        {
            GUILayout.BeginHorizontal();
            _items.TryGetValue(i, out int value);
            if (GUILayout.Button("-", GUILayout.Width(20f)))
                _items[i] = value - 1;
            if (GUILayout.Button("+", GUILayout.Width(20f)))
                ReceivedItem(i);
            var has_item = value > 0;
            var will_have_item = GUILayout.Toggle(has_item, "", GUILayout.Width(15f));
            if (has_item != will_have_item)
            {
                if (will_have_item)
                    ReceivedItem(i);
                else if (value == 1)
                    _items.Remove(i);
            }
            GUILayout.Label(value.ToString(), GUILayout.Width(25f));
            var has_location = _locations.Contains(i);
            var will_have_location = GUILayout.Toggle(has_location, "", GUILayout.Width(15f));
            if (has_location != will_have_location)
            {
                if (will_have_location)
                    ReceivedLocation(i);
                else
                    _locations.Remove(i);
            }
            GUILayout.Label(i.ToString(), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    public bool HasItem(CheckId id, int atLeast = 1)
    {
        return _items.TryGetValue(id, out var value) && value >= atLeast;
    }
    public bool HasLocation(CheckId id)
    {
        return _locations.Contains(id);
    }

    public void ReceivedItem(CheckId id)
    {
        // TODO: cleaner way of this
        if (!_items.ContainsKey(id))
            _items[id] = 0;
        ++_items[id];
        Plugin.BepInLogger.LogDebug($"received item {id}");
        // TODO: any immediately-occuring updates go here
    }
    public void ReceivedLocation(CheckId id)
    {
        if (!_locations.Add(id))
            return;
        Plugin.BepInLogger.LogDebug($"received location {id}");
        // TODO: update in-level trackers
    }
    public void FoundLocation(CheckId id)
    {
        if (!_allLocations.Contains(id))
        {
            Plugin.BepInLogger.LogDebug($"Ignoring location {id} because it is not valid");
            return;
        }
        Plugin.BepInLogger.LogDebug($"found location {id}");
        ReceivedLocation(id);
        Plugin.ArchipelagoClient.SendLocation(id);
    }
}
