using DonutCountyAP.Randomizer;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Events;

namespace DonutCountyAP.Patches;

public partial class GlobalPatches
{
    // TODO: add region data storage (poptracker)

    // TODO: split check granting into their own patches

    [HarmonyPatch(typeof(SceneManager), "OnLevelComplete"), HarmonyPrefix]
    static void SceneManager_OnLevelComplete()
    {
        var currentInfo = ExtraDeliveryInfo.GetCurrent(out var delivery);
        if (currentInfo.EndOfLevel)
        {
            DataManager.GetCurrentDeliveryData().nextScene = "titlescreen";
            Plugin.GameState.ActiveDelivery = false;
        }
        Plugin.GameState.FoundEvent($"delivery{delivery}", true);
    }

    [HarmonyPatch(typeof(SceneManager), "OnQueueLevel"), HarmonyPrefix]
    static void SceneManager_OnQueueLevel(ref string __0)
    {
        if (__0 == "999ft")
        {
            Plugin.BepInLogger.LogMessage("saving you from the depths");
            __0 = "titlescreen";
        }
        if (__0 == "999ft_trap")
            __0 = "999ft";
        if (Plugin.GameState == null)
            return;
        Plugin.GameState.ActiveDelivery = __0 != "titlescreen" && __0 != "results" && __0 != "scn_credits" && __0 != "999ft";
    }

    [HarmonyPatch(typeof(Tornado), "Awake"), HarmonyPostfix]
    static void Tornado_Awake(Tornado __instance)
    {
        __instance.onCompleteEvent.AddListener(() => Plugin.GameState.FoundEvent("boss_tornado"));
    }

    [HarmonyPatch(typeof(CameraManager), "MoveCamera"), HarmonyPrefix]
    static void CameraManager_MoveCamera(CameraManager __instance)
    {
        // this is called outside of gameplay
        if (Plugin.GameState == null)
            return;
        var currentInfo = ExtraDeliveryInfo.GetCurrent(out var delivery);
        if (currentInfo == null)
            return;
        // in deliveries, index is always the next camera
        var camera = __instance.GetIndex();
        var eventId = $"delivery{delivery}camera{camera}";
        Plugin.GameState.FoundEvent(eventId, true);
    }

    [HarmonyPatch(typeof(OS1Store), "OnPressDoneButton"), HarmonyPrefix]
    static void OS1Store_OnPressDoneButton()
    {
        Plugin.GameState.FoundEvent("store_done");
    }

    [HarmonyPatch(typeof(TKOfficeManager), "Start"), HarmonyPostfix]
    static void TKOfficeManager_Start(TKOfficeManager __instance)
    {
        __instance.spotlights[2].onCompleteBTC.onPlay.AddListener(() => Plugin.GameState.FoundEvent("tk_office2"));
        __instance.spotlights[5].onCompleteBTC.onPlay.AddListener(() => Plugin.GameState.FoundEvent("tk_office5"));
        __instance.spotlights[8].onCompleteBTC.onPlay.AddListener(() => Plugin.GameState.FoundEvent("tk_office8"));
    }

    [HarmonyPatch(typeof(TKOfficeManager), "JailRoutine"), HarmonyPrefix]
    static void TKOfficeManager_JailRoutine()
    {
        Plugin.GameState.FoundEvent("tk_office_jail");
    }

    [HarmonyPatch(typeof(QuadcopterBigBoy), "Entrance_Enter"), HarmonyPrefix]
    static void QuadcopterBigBoy_Entrance_Enter()
    {
        Plugin.GameState.FoundEvent("quadcopter_big_boy");
    }


}
