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
        var current_info = ExtraDeliveryInfo.GetCurrent();
        if (current_info.EndOfLevel)
        {
            DataManager.GetCurrentDeliveryData().nextScene = "titlescreen";
            Plugin.GameState.ActiveDelivery = false;
        }
        if (current_info.FinishLocation != CheckId.None)
            Plugin.GameState.FoundLocation(current_info.FinishLocation);
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
        __instance.onCompleteEvent.AddListener(() => Plugin.GameState.FoundLocation(CheckId.DeliveryBossFight));
    }

    [HarmonyPatch(typeof(CameraManager), "MoveCamera"), HarmonyPrefix]
    static void CameraManager_MoveCamera(CameraManager __instance)
    {
        // this is called outside of gameplay
        if (Plugin.GameState == null)
            return;
        var current_info = ExtraDeliveryInfo.GetCurrent();
        if (current_info == null)
            return;
        // in deliveries, index is always the next camera
        var index = __instance.GetIndex() - 1;
        if (index < 0 || index >= current_info.StartCameraLocations.Length)
            return;
        var check = current_info.StartCameraLocations[index];
        if (check == CheckId.None)
            return;
        Plugin.GameState.FoundLocation(check);
    }

    [HarmonyPatch(typeof(OS1Store), "OnPressDoneButton"), HarmonyPrefix]
    static void OS1Store_OnPressDoneButton()
    {
        if (Plugin.GameState.Options.LevelSegments)
            Plugin.GameState.FoundLocation(CheckId.SegmentChickenBarn2);
    }

    [HarmonyPatch(typeof(TKOfficeManager), "Start"), HarmonyPostfix]
    static void TKOfficeManager_Start(TKOfficeManager __instance)
    {
        __instance.spotlights[2].onCompleteBTC.onPlay.AddListener(() => Plugin.GameState.FoundLocation(CheckId.SegmentTrashKingsOffice1));
        __instance.spotlights[5].onCompleteBTC.onPlay.AddListener(() => Plugin.GameState.FoundLocation(CheckId.SegmentTrashKingsOffice2));
        __instance.spotlights[8].onCompleteBTC.onPlay.AddListener(() => Plugin.GameState.FoundLocation(CheckId.SegmentTrashKingsOffice3));
    }

    [HarmonyPatch(typeof(TKOfficeManager), "JailRoutine"), HarmonyPrefix]
    static void TKOfficeManager_JailRoutine()
    {
        Plugin.GameState.FoundLocation(CheckId.DeliveryTrashKingsOffice);
    }

    [HarmonyPatch(typeof(QuadcopterBigBoy), "Entrance_Enter"), HarmonyPrefix]
    static void QuadcopterBigBoy_Entrance_Enter()
    {
        Plugin.GameState.FoundLocation(CheckId.SegmentThe4053);
    }

    [HarmonyPatch(typeof(LevelSettings), "Start"), HarmonyPostfix]
    static void Postfix(LevelSettings __instance)
    {
        if (__instance.deliveryData?.name == "DonutshopEpiloque")
            Plugin.GameState.FoundLocation(CheckId.Victory);
    }


}
