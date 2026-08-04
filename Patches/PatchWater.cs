using DonutCountyAP.Randomizer;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DonutCountyAP.Patches;

public class PatchWater
{

    [HarmonyPatch(typeof(HoleContents), "SetWater")]
    public static class HoleContents_SetWater
    {
        static bool Prefix(bool __0)
        {
            // water item is needed to collect water
            return !__0 || Plugin.GameState.HasItem(CheckId.HoleWater);
        }
    }

    [HarmonyPatch(typeof(WaterVolume), "Drain")]
    public static class WaterVolume_Drain
    {
        static bool Prefix()
        {
            // water item is needed to drain water
            return Plugin.GameState.HasItem(CheckId.HoleWater);
        }
    }
}
