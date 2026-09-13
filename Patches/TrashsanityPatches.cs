using DonutCountyAP.Randomizer;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DonutCountyAP.Patches;

public class TrashsanityPatches
{
    [HarmonyPatch(typeof(LevelSettings), "Awake"), HarmonyPrefix]
    static void LevelSettings_Awake(LevelSettings __instance)
    {
        if (__instance.deliveryData != null && __instance.deliveryData != DataManager.GetCurrentDeliveryData())
            return;
        var i = 0;
        var sceneName = __instance.gameObject.scene.name;
        if (!Plugin.Logic.Trash.TryGetValue(sceneName, out var expected))
            return;
        Plugin.BepInLogger.LogDebug("prepare for lag");
        foreach (var obj in __instance.gameObject.scene.GetRootGameObjects())
            EnumerateTransform(obj.transform, sceneName, expected, Plugin.GameState.Options.Trashsanity == GameOptions.TrashsanityMode.Types, ref i, null);
        if (i != expected.Length)
            Plugin.BepInLogger.LogError($"bad trash {sceneName} count {i} expected {expected.Length}");
        Plugin.BepInLogger.LogDebug($"loaded {i} fallstate(s) in {sceneName}");

    }

    static void EnumerateTransform(Transform transform, string sceneName, Logic.GameTrash[] expected, bool types, ref int i, FallStateExtra parent)
    {
        var fallState = transform.GetComponent<FallState>();
        if (fallState != null)
        {
            var data = expected[i];
            if (data.Name != fallState.name)
            {
                Plugin.BepInLogger.LogError($"bad trash {sceneName}[{i}] {fallState.name} expected {data.Name}");
            } else
            {
                parent = fallState.gameObject.AddComponent<FallStateExtra>();
                parent.Unlock = data.Unlock;
                parent.Location = types ? data.TypeId : data.Id;
                parent.DebugIndex = i;
                parent.Refresh();
            }
            ++i;
        }
        if (parent != null)
        {
            Renderer renderer = transform.GetComponent<MeshRenderer>();
            renderer ??= transform.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
                parent.AddChildRenderer(renderer);
            foreach (var collider in transform.GetComponents<Collider>())
                parent.AddChildCollider(collider);
        }
        foreach (Transform child in transform)
            EnumerateTransform(child, sceneName, expected, types, ref i, parent);
    }

    [HarmonyPatch(typeof(HoleContents), "AddToHole"), HarmonyPrefix]
    static void HoleContents_AddToHole(GameObject p)
    {
        p.GetComponent<FallStateExtra>()?.Collect();
    }

    [HarmonyPatch(typeof(Eat), "OnDestroyOther"), HarmonyPrefix]
    static void Eat_OnDestroyOther(GameObject g)
    {
        Plugin.BepInLogger.LogDebug($"eat destroy {g}");
        g.GetComponent<FallStateExtra>()?.Collect();
    }

    [HarmonyPatch(typeof(FallState), "Start"), HarmonyPrefix]
    static void FallState_Start(FallState __instance)
    {
        if (__instance.GetComponent<FallStateExtra>() == null)
        {
            var id = $"{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} {__instance.name}";
            switch (id)
            {
                case "sbx_soup kitchenCockroach(Clone)": // Cockroach
                case "scn_popcorn_simple Popcorn": // Corn
                case "scn_fireworks popcorn(Clone)": // Corn
                case "scn_waterpark fish(Clone)": // Fish
                case "scn_waterpark waterBalloon(Clone)": // WaterBalloon
                case "scn_405 photo(Clone)": // Camera
                default: 
                    Plugin.BepInLogger.LogError($"object {id} is late!");
                    break;
            }
        }
    }
}
