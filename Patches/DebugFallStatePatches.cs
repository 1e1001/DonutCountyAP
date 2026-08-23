using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DonutCountyAP.Patches;

// development utilities for getting fallstates in a level (trashsanity)
public class DebugFallStatePatches
{
    public record struct FallStateInfo(string Name, int Count, int Camera);
    public static Dictionary<string, Dictionary<int, FallStateInfo>> ObjectList = [];

    public static int CurrentSceneIndex = 0;
    public static int CurrentCamera = 9999;
    public static bool CurrentStartup = false;
    public static string CurrentScene = "";

    public class DebugFallState : MonoBehaviour
    {
        // TODO: i need a better way of identifying fallstates that can be:
        // - stable (and identifiable when it isn't)
        // - searchable in the editor
        // - maybe usable as check names?
        public string Scene;
        public int Index;

        void Start()
        {
            Plugin.BepInLogger.LogDebug($"WOKE object {gameObject} with id {Index}");
            Scene = this.gameObject.scene.name;
            if (!ObjectList.ContainsKey(Scene))
                ObjectList.Add(Scene, []);
            var sceneList = ObjectList[Scene];
            if (!sceneList.ContainsKey(Index))
                sceneList.Add(Index, new(gameObject.name, 0, 9999));
        }

        public void AddToHole()
        {
            var oldEntry = ObjectList[Scene][Index];
            ObjectList[Scene][Index] = new(oldEntry.Name, oldEntry.Count + 1, Math.Min(oldEntry.Camera, RM.cameraManager.GetIndex()));
        }
    }

    [HarmonyPatch(typeof(HoleContents), "AddToHole"), HarmonyPrefix]
    static void HoleContents_AddToHole(GameObject p)
    {
        p.GetComponent<DebugFallState>().AddToHole();
    }

    [HarmonyPatch(typeof(SceneManager), "OnQueueLevel"), HarmonyPrefix]
    static void SceneManager_OnQueueLevel()
    {
        CurrentStartup = true;
    }

    [HarmonyPatch(typeof(LevelSettings), "Start"), HarmonyPrefix]
    static void LevelSettings_Start(LevelSettings __instance)
    {
        var scene = __instance.gameObject.scene.name;
        if (scene == CurrentScene)
        {
            Plugin.BepInLogger.LogWarning("LEVELSETTINGS LOADED TWICE?");
        }
        CurrentScene = scene;
        CurrentSceneIndex = 0;
        var objs = UnityEngine.Object.FindObjectsOfType<FallState>();
        foreach (var obj in objs)
        {
            Plugin.BepInLogger.LogDebug($"object {obj.gameObject} with id {CurrentSceneIndex}");
            obj.gameObject.AddComponent<DebugFallState>().Index = CurrentSceneIndex++;
        }
        CurrentSceneIndex = 0;
        CurrentStartup = false;
    }


    [HarmonyPatch(typeof(FallState), "Start"), HarmonyPrefix]
    static void FallState_Start(FallState __instance)
    {
        if (__instance.GetComponent<DebugFallState>() == null)
        {
            __instance.gameObject.AddComponent<DebugFallState>().Index = --CurrentSceneIndex;
            Plugin.BepInLogger.LogDebug($"LATE object {__instance.gameObject} with id {CurrentSceneIndex}");
        }
    }
}
