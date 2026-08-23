using DonutCountyAP.Patches;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DonutCountyAP.Randomizer;

public partial class GameState
{
    public readonly int[] _inventory = new int[(int)ItemId.Length];
    public readonly bool[] _locations = new bool[AutoLogic.LOCATIONS_SIZE];
    public bool ActiveDelivery = false;

    public GameOptions Options;

    public GameState(GameOptions options) {
        Options = options;
        Options.ApplyPatches();
    }


    private Rect _guiRect = new(100, 100, 400, 400);
    private Vector2 _guiScroll;
    private string _guiOptionsText = null;
    private string _guiDebugKeyText = "";
    private string _guiDebugValueText = "";
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
        var debugClient = Plugin.Client as DebugClient;
        if (debugClient != null)
        {
            foreach (var entry in debugClient.FakeRandomizer)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("-", GUILayout.Width(20f)))
                    debugClient.FakeRandomizer.Remove(entry.Key);
                GUILayout.Label($"{entry.Key} -> {entry.Value}");
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+", GUILayout.Width(20f)) && long.TryParse(_guiDebugKeyText, out var location))
                debugClient.FakeRandomizer.Add(location, (ItemId)Enum.Parse(typeof(ItemId), _guiDebugValueText));
            _guiDebugKeyText = GUILayout.TextField(_guiDebugKeyText, GUILayout.ExpandWidth(false), GUILayout.MinWidth(100));
            GUILayout.Label(" -> ", GUILayout.ExpandWidth(false));
            _guiDebugValueText = GUILayout.TextField(_guiDebugValueText, GUILayout.ExpandWidth(false), GUILayout.MinWidth(100));
            GUILayout.EndHorizontal();
        }
        GUILayout.BeginHorizontal();
        var patchEnabled = Plugin.Patcher.DebugFallState.Enabled;
        Plugin.Patcher.DebugFallState.Set(GUILayout.Toggle(patchEnabled, "fallstate"));
        GUILayout.EndHorizontal();
        if (patchEnabled)
        {
            GUILayout.TextArea(JsonConvert.SerializeObject(DebugFallStatePatches.ObjectList, Formatting.Indented));
        }
        GUILayout.Label("items");
        foreach (ItemId id in AutoLogic.DEBUG_SORTED_ITEMS)
        {
            GUILayout.BeginHorizontal();
            var value = _inventory[(int)id];
            if (GUILayout.Button("-", GUILayout.Width(20f)))
                _inventory[(int)id] = value - 1;
            if (GUILayout.Button("+", GUILayout.Width(20f)))
                ReceivedItem(id);
            var has_item = value > 0;
            var will_have_item = GUILayout.Toggle(has_item, "", GUILayout.Width(15f));
            if (has_item != will_have_item)
            {
                if (will_have_item)
                    ReceivedItem(id);
                else if (value == 1)
                    _inventory[(int)id] = 0;
            }
            GUILayout.Label(value.ToString(), GUILayout.Width(25f));
            GUILayout.Label(id.ToString(), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
        GUILayout.Label("locations");
        foreach (AutoLogic.DebugTracker entry in AutoLogic.DEBUG_TRACKER)
        {
            GUILayout.BeginHorizontal();
            var has_location = _locations[entry.Location.Id];
            var will_have_location = GUILayout.Toggle(has_location, "", GUILayout.Width(15f));
            _locations[entry.Location.Id] = will_have_location;
            if (has_location != will_have_location && will_have_location)
                ReceivedLocation(entry.Location.Id);
            var oldColor = GUI.contentColor;
            if (!Options.CanSendLocation(entry.Location.Type))
                GUI.contentColor = Color.grey;
            GUILayout.Label(entry.Name, GUILayout.ExpandWidth(false));
            GUI.contentColor = oldColor;
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
        // crying
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    public bool Has(ItemId id, int atLeast = 1)
    {
        return _inventory[(int)id]  >= atLeast;
    }
    public int Quantity(ItemId id)
    {
        return _inventory[(int)id];
    }

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
        switch(id)
        {
            // these should exist at all times, so if they somehow don't it's fine to drop the item
            case ItemId.FillerBackflip:
                RM.gameUI?.GetComponent<Backflip>()?.DoBackflip();
                break;
            case ItemId.CementTrap:
                if (HasLocation(AutoLogic.LOCATION_GOAL))
                    break;
                RM.substanceManager?.GetComponent<CementTrap>()?.DoCementTrap();
                break;
            case ItemId.DepthsTrap:
                if (HasLocation(AutoLogic.LOCATION_GOAL))
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
        }
    }
    public void ReceivedLocation(int id, bool local = false)
    {
        if (_locations[id])
            return;
        if (local)
        {
            if (id == AutoLogic.LOCATION_GOAL)
                Plugin.Client.SendGoal();
            else
                Plugin.Client.SendLocation(id);
        }
        _locations[id] = true;
        Plugin.BepInLogger.LogDebug($"received location {id}");
        // TODO: any more immediately-occuring updates go here
    }
    public void FoundEvent(string name, bool allowInvalid = false)
    {
        if (!AutoLogic.EVENTS.TryGetValue(name, out var location))
        {
            if (!allowInvalid)
                Plugin.BepInLogger.LogWarning($"triggered invalid event {name}");
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
