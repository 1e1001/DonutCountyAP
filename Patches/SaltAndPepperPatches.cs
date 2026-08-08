using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DonutCountyAP.Patches;
public class SaltAndPepperPatches
{

    static readonly FieldInfo SoupManager_numSalt = AccessTools.Field(typeof(SoupManager), "numSalt");
    static readonly FieldInfo SoupManager_numPepper = AccessTools.Field(typeof(SoupManager), "numPepper");

    [HarmonyPatch(typeof(SoupManager), "TestSecret"), HarmonyPrefix]
    static void SoupManager_TestSecret(SoupManager __instance)
    {
        Plugin.GameState.FoundEvent($"soup_salt{SoupManager_numSalt.GetValue(__instance)}");
        Plugin.GameState.FoundEvent($"soup_pepper{SoupManager_numPepper.GetValue(__instance)}");
    }

    // TODO: realtime salt/pepper items (might need modification of the above patch)
}
