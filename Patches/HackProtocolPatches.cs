using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DonutCountyAP.Patches;
public class HackProtocolPatches
{
    [HarmonyPatch(typeof(Hackable), "OnHackAttempt"), HarmonyPrefix]
    static void Hackable_OnHackAttempt(Hackable __instance)
    {
        switch (__instance.gameObject.name)
        {
            case "HackerDeviceTrigger":
                Plugin.GameState.FoundEvent("hack_hq");
                break;
            case "Boss Quadcopter":
                break;
            default:
                Plugin.BepInLogger.LogWarning($"got invalid hackable device: {__instance.gameObject.name}");
                break;
        }
    }

    // TODO: realtime hacking patch
}
