using DonutCountyAP.Randomizer;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DonutCountyAP.Patches;

public partial class GlobalPatches
{

    static IEnumerator NullCoroutine()
    {
        yield return null;
    }

    [HarmonyPatch(typeof(HoleContents), "SetWater"), HarmonyPrefix]
    static bool HoleContents_SetWater(bool __0)
    {
        return !__0 || !Plugin.GameState.Options.HoleWater || Plugin.GameState.Has(ItemId.HoleWater);
    }

    [HarmonyPatch(typeof(WaterVolume), "Drain"), HarmonyPrefix]
    static bool WaterVolume_Drain()
    {
        return !Plugin.GameState.Options.HoleWater || Plugin.GameState.Has(ItemId.HoleWater);
    }

    [HarmonyPatch(typeof(SoupManager), "SetBroth"), HarmonyPrefix]
    static bool SoupManager_SetBroth(bool __0)
    {
        return !__0 || !Plugin.GameState.Options.HoleWater || Plugin.GameState.Has(ItemId.HoleWater);
    }

    [HarmonyPatch(typeof(HoleContents), "SetFire"), HarmonyPrefix]
    static bool HoleContents_SetFire(bool __0)
    {
        return !__0 || !Plugin.GameState.Options.HoleFire || Plugin.GameState.Has(ItemId.HoleFire);
    }

    [HarmonyPatch(typeof(HoleContents), "MakePopcorn"), HarmonyPrefix]
    static bool HoleContents_MakePopcorn(ref IEnumerator __result)
    {
        __result = NullCoroutine();
        return !Plugin.GameState.Options.HoleFire || Plugin.GameState.Has(ItemId.HoleFire);
    }

    [HarmonyPatch(typeof(HoleContents), "LaunchRocket"), HarmonyPrefix]
    static bool HoleContents_LaunchRocket(ref IEnumerator __result)
    {
        __result = NullCoroutine();
        return !Plugin.GameState.Options.HoleFire || Plugin.GameState.Has(ItemId.HoleFire);
    }

    [HarmonyPatch(typeof(HoleSnake), "SetSnakeHole"), HarmonyPrefix]
    static bool HoleSnake_SetSnakeHole(bool __0)
    {
        return !__0 || !Plugin.GameState.Options.HoleSnake || Plugin.GameState.Has(ItemId.HoleSnake);
    }

    [HarmonyPatch(typeof(Flashlight), "OnCollectBattery"), HarmonyPrefix]
    static bool Flashlight_OnCollectBattery()
    {
        return !Plugin.GameState.Options.HoleLight || Plugin.GameState.Has(ItemId.HoleLight);
    }

    [HarmonyPatch(typeof(CheckForNumberOfBunnies), "OnCollectBunny"), HarmonyPrefix]
    static bool CheckForNumberOfBunnies_OnCollectBunny()
    {
        // technically this only patches out the first set of bunnies, but you can't progress without it
        return !Plugin.GameState.Options.HoleBunnies || Plugin.GameState.Has(ItemId.HoleBunnies);
    }
}
