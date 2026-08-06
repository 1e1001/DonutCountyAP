using DonutCountyAP.Randomizer;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DonutCountyAP.Patches;

public class HoleWaterPatches
{

    [HarmonyPatch(typeof(HoleContents), "SetWater"), HarmonyPrefix]
    static bool HoleContents_SetWater(bool __0)
    {
        // water item is needed to collect water
        return !__0 || Plugin.GameState.HasItem(CheckId.HoleWater);
    }

    [HarmonyPatch(typeof(WaterVolume), "Drain"), HarmonyPrefix]
    static bool WaterVolume_Drain()
    {
        // water item is needed to drain water
        return Plugin.GameState.HasItem(CheckId.HoleWater);
    }

    [HarmonyPatch(typeof(SoupManager), "SetBroth"), HarmonyPrefix]
    static bool SoupManager_SetBroth(bool __0)
    {
        // water item is needed to collect soup or spices
        return !__0 || Plugin.GameState.HasItem(CheckId.HoleWater);
    }
}
