using DonutCountyAP.Randomizer;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace DonutCountyAP.Patches;
public class SnakeDangerPatches
{

    [HarmonyPatch(typeof(RangerRadio), "OnDangerEvent"), HarmonyPrefix]
    static bool RangerRadio_OnDangerEvent(RangerRadio __instance)
    {
        // too lazy to patch each caller, so here's awful hacks
        var trace = new StackTrace(false);
        //Plugin.BepInLogger.LogDebug($"got stacktrace: {trace.GetFrame(2).GetMethod().DeclaringType.Name}");
        switch (trace.GetFrame(2).GetMethod().DeclaringType.Name)
        {
            case "<DangerRoutine>c__Iterator0":
                // requeue danger animation
                OnDangerEvent(__instance);
                break;
            case "<SnakeMoveRoutine>c__Iterator0":
                Plugin.GameState.FoundEvent("snake_danger_snake");
                // queue initial snake danger
                OnDangerEvent(__instance);
                break;
            case "ChickenSnakeAlarm":
                Plugin.GameState.FoundEvent("snake_danger_horn");
                break;
            case "ChickenSign":
                Plugin.GameState.FoundEvent("snake_danger_sign");
                break;
            case "ChickenSwing":
                Plugin.GameState.FoundEvent("snake_danger_swing");
                break;
            default:
                Plugin.BepInLogger.LogWarning($"got invalid stacktrace for danger event: {trace}");
                break;
        }
        return false;
    }

    static readonly FieldInfo RangerRadio_dangerCoroutine = AccessTools.Field(typeof(RangerRadio), "dangerCoroutine");
    static readonly FieldInfo RangerRadio_dangerEventQueue = AccessTools.Field(typeof(RangerRadio), "dangerEventQueue");
    static readonly FieldInfo RangerRadio__exceededMaxDangerLevel = AccessTools.Field(typeof(RangerRadio), "_exceededMaxDangerLevel");
    static readonly FieldInfo RangerRadio_dangerLevel = AccessTools.Field(typeof(RangerRadio), "dangerLevel");
    static readonly MethodInfo RangerRadio_DangerRoutine = AccessTools.Method(typeof(RangerRadio), "DangerRoutine");

    public static void OnDangerEvent(RangerRadio __instance)
    {
        int dangerPoints = Math.Min(Plugin.GameState.Quantity(ItemId.SnakeDanger), 4);
        // danger event queue is bugged anyways, so just use 0 and replace it with my own counter
        if (RangerRadio_dangerCoroutine.GetValue(__instance) != null)
            ((List<int>)RangerRadio_dangerEventQueue.GetValue(__instance)).Add(0);
        else if (!(bool)RangerRadio__exceededMaxDangerLevel.GetValue(__instance) && dangerPoints != (int)RangerRadio_dangerLevel.GetValue(__instance))
            RangerRadio_dangerCoroutine.SetValue(__instance, __instance.StartCoroutine((IEnumerator)RangerRadio_DangerRoutine.Invoke(__instance, [dangerPoints])));
    }
}
