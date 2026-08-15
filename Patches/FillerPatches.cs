using DonutCountyAP.Randomizer;
using HarmonyLib;
using UnityEngine;

namespace DonutCountyAP.Patches;

public partial class GlobalPatches
{
    [HarmonyPatch(typeof(OS1GameUI), "Start"), HarmonyPostfix]
    static void OS1GameUI_Start(OS1GameUI __instance)
    {
        var backflip = __instance.gameObject.AddComponent<Backflip>();
        backflip.Characters = new GameObject[__instance.characters.Length];
        for (int i = 0; i < __instance.characters.Length; ++i)
            backflip.Characters[i] = __instance.characters[i]._characterHolder;
    }

    [HarmonyPatch(typeof(HoleSubstanceManager), "Start"), HarmonyPostfix]
    static void HoleSubstanceManager_Start(HoleSubstanceManager __instance)
    {
        __instance.gameObject.AddComponent<CementTrap>();
    }

    [HarmonyPatch(typeof(HoleSubstanceManager), "SetSubstance"), HarmonyPrefix]
    static bool HoleSubstanceManager_SetSubstance(HoleSubstanceManager __instance, HoleSubstanceManager.Substance __0)
    {
        var trap = __instance.GetComponent<CementTrap>();
        if (trap == null)
            return true;
        if (trap.Timer <= 0)
            return true;
        trap.UnderlyingSubstance = __0;
        return false;
    }

    public static bool DepthsTrapReroll = true;

    [HarmonyPatch(typeof(OS1UndergroundManager), "GetCurrentBlock"), HarmonyPrefix]
    static void OS1UndergroundManager_GetCurrentBlock(OS1UndergroundManager __instance)
    {
        __instance.useDebugIndex = true;
        if (!DepthsTrapReroll)
            return;
        DepthsTrapReroll = false;
        __instance.debugIndex = UnityEngine.Random.Range(2, 18);
    }
}
