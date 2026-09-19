using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DonutCountyAP.Randomizer;

public class ClientOptions
{
    [JsonIgnore]
    public bool Stale = false;
    [JsonIgnore]
    public object CacheLock = new();

    public int TrashopediaIndex;
    public string Uri = "localhost";
    public string SlotName = "Player1";
    public string Password;
    public bool EasierAchievements;
    public bool DialogueSkipping = true;
    public bool TrashTrackers = true;
    // cache un-confirmed locations between game sessions, in case of spurious disconnects or crashes
    public string CacheId;
    public HashSet<long> CacheLocations = [];
    public int CacheReceived;

    public void Validate() { }
    public void ApplyPatches()
    {
        Plugin.Patcher.EasierAchievements.Set(EasierAchievements);
        Plugin.Patcher.DialogueSkipping.Set(DialogueSkipping);
    }
    public void Log()
    {
        Plugin.BepInLogger.LogDebug($"trashopedia_index: {TrashopediaIndex}");
        Plugin.BepInLogger.LogDebug($"connection: {Uri}, {SlotName}");
        Plugin.BepInLogger.LogDebug($"flags: {EasierAchievements} {DialogueSkipping} {TrashTrackers}");
    }

    public void OnGUI() {
        GUI.Label(new Rect(16, 170, 300, 20), "Options:");
        EasierAchievements = GUI.Toggle(new Rect(16, 190, 300, 20), EasierAchievements, "Reduce requirements for slow achievements");
        DialogueSkipping = GUI.Toggle(new Rect(16, 210, 300, 20), DialogueSkipping, "Allow skipping texting scenes and fast-forwarding dialogue");
        TrashTrackers = GUI.Toggle(new Rect(16, 230, 300, 20), TrashTrackers, "Markers on uncollected trash");
        if (GUI.Button(new Rect(16, 250, 150, 20), "Apply"))
        {
            DataManager.SaveGameData_Steam();
            ApplyPatches();
        }
    }
}
