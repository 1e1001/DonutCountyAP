using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DonutCountyAP.Patches;
public class SnakeDangerPatches
{

    [HarmonyPatch(typeof(RangerRadio), "OnDangerEvent"), HarmonyPrefix]
    static void RangerRadio_OnDangerEvent()
    {
        // too lazy to patch each sub-item, so here's awful hacks
        var trace = new StackTrace(false);
        switch (trace.GetFrame(2).GetMethod().Name)
        {
            // this also runs when requeueing danger animation, but you always get the snake first
            case "MoveNext":
                Plugin.GameState.FoundEvent("snake_danger_snake");
                break;
            case "Honk":
                Plugin.GameState.FoundEvent("snake_danger_horn");
                break;
            case "Break":
                Plugin.GameState.FoundEvent("snake_danger_sign");
                break;
            case "OnComplete":
                Plugin.GameState.FoundEvent("snake_danger_swing");
                break;
            default:
                Plugin.BepInLogger.LogWarning($"got invalid stacktrace for danger event: {trace}");
                break;
        }
    }

    // TODO: realtime snake danger items
}
