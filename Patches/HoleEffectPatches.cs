using DonutCountyAP.Randomizer;
using HarmonyLib;
using System.Collections;

namespace DonutCountyAP.Patches;

public partial class GlobalPatches
{

    static IEnumerator NullCoroutine()
    {
        yield return null;
    }

    [HarmonyPatch(typeof(HoleContents), "SetWater"), HarmonyPrefix]
    static bool HoleContents_SetWater(bool state)
    {
        return !state || (Plugin.GameState.HasHole(ItemId.HoleWater) && CementTrap.HasNoCement());
    }

    [HarmonyPatch(typeof(WaterVolume), "Drain"), HarmonyPrefix]
    static bool WaterVolume_Drain()
    {
        return Plugin.GameState.HasHole(ItemId.HoleWater) && CementTrap.HasNoCement();
    }

    [HarmonyPatch(typeof(SoupManager), "SetBroth"), HarmonyPrefix]
    static bool SoupManager_SetBroth(bool state)
    {
        return !state || (Plugin.GameState.HasHole(ItemId.HoleWater) && CementTrap.HasNoCement());
    }

    [HarmonyPatch(typeof(HoleContents), "SetFire"), HarmonyPrefix]
    static bool HoleContents_SetFire(bool state)
    {
        return !state || Plugin.GameState.HasHole(ItemId.HoleFire);
    }

    [HarmonyPatch(typeof(HoleContents), "MakePopcorn"), HarmonyPrefix]
    static bool HoleContents_MakePopcorn(ref IEnumerator __result)
    {
        __result = NullCoroutine();
        return Plugin.GameState.HasHole(ItemId.HoleFire);
    }

    [HarmonyPatch(typeof(HoleContents), "LaunchRocket"), HarmonyPrefix]
    static bool HoleContents_LaunchRocket(ref IEnumerator __result)
    {
        __result = NullCoroutine();
        return Plugin.GameState.HasHole(ItemId.HoleFire);
    }

    [HarmonyPatch(typeof(HoleSnake), "SetSnakeHole"), HarmonyPrefix]
    static bool HoleSnake_SetSnakeHole(bool on)
    {
        return !on || Plugin.GameState.HasHole(ItemId.HoleSnake);
    }

    [HarmonyPatch(typeof(Flashlight), "OnCollectBattery"), HarmonyPrefix]
    static bool Flashlight_OnCollectBattery()
    {
        return Plugin.GameState.HasHole(ItemId.HoleLight);
    }

    [HarmonyPatch(typeof(CheckForNumberOfBunnies), "OnCollectBunny"), HarmonyPrefix]
    static bool CheckForNumberOfBunnies_OnCollectBunny()
    {
        // technically this only patches out the first set of bunnies, but you can't progress without it in either level
        return Plugin.GameState.HasHole(ItemId.HoleBunnies);
    }
}
