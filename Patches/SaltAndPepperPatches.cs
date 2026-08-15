using DonutCountyAP.Randomizer;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace DonutCountyAP.Patches;
public class SaltAndPepperPatches
{

    static readonly FieldInfo SoupManager_numSalt = AccessTools.Field(typeof(SoupManager), "numSalt");
    static readonly FieldInfo SoupManager_numPepper = AccessTools.Field(typeof(SoupManager), "numPepper");
    static readonly FieldInfo SoupManager_targetColor = AccessTools.Field(typeof(SoupManager), "targetColor");

    static Color GetColor(SoupManager soup, int salt, int pepper)
    {
        if (salt >= 2 && pepper >= 3)
            return soup.secretColor;
        else if (salt >= 1 && pepper >= 1)
            return soup.completeColor;
        else if (salt >= 1 || pepper >= 1)
            return soup.seasonedColor;
        else
            return soup.brothColor;
    }

    [HarmonyPatch(typeof(SoupManager), "TestSecret"), HarmonyPrefix]
    public static bool SoupManager_TestSecret(SoupManager __instance)
    {
        if (!__instance.isBad)
        {
            Plugin.GameState.FoundEvent($"soup_salt{SoupManager_numSalt.GetValue(__instance)}");
            Plugin.GameState.FoundEvent($"soup_pepper{SoupManager_numPepper.GetValue(__instance)}");
        }
        var salt = Plugin.GameState.Quantity(ItemId.Salt);
        var pepper = Plugin.GameState.Quantity(ItemId.Pepper);
        SoupManager_targetColor.SetValue(__instance, GetColor(__instance, salt, pepper));
        return false;
    }

    [HarmonyPatch(typeof(SoupManager), "SetBroth"), HarmonyPostfix]
    static void SoupManager_SetBroth(SoupManager __instance)
    {
        __instance.hasSalt = true;
        __instance.hasPepper = true;
        SoupManager_TestSecret(__instance);
    }

    [HarmonyPatch(typeof(SoupManager), "GetWasLastSoupBad"), HarmonyPostfix]
    static void SoupManager_GetWasLastSoupBad(ref bool __result)
    {
        var is_good = Plugin.GameState.Has(ItemId.Salt, 1) && Plugin.GameState.Has(ItemId.Pepper, 1);
        __result |= !is_good;
    }

    [HarmonyPatch(typeof(SoupManager), "GetWasLastSoupSecret"), HarmonyPostfix]
    static void SoupManager_GetWasLastSoupSecret(ref bool __result)
    {
        __result = Plugin.GameState.Has(ItemId.Salt, 2) && Plugin.GameState.Has(ItemId.Pepper, 3);
    }
}
