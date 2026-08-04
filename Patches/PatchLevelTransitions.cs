using DonutCountyAP.Randomizer;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Events;

namespace DonutCountyAP.Patches;

public class PatchLevelTransitions
{
    // TODO: add region data storage (poptracker)

    [HarmonyPatch(typeof(SceneManager), "OnLevelComplete")]
    public static class SceneManager_OnLevelComplete
    {
        static void Prefix()
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
    }

    [HarmonyPatch(typeof(SceneManager), "OnQueueLevel")]
    public static class SceneManager_OnQueueLevel
    {
        static void Prefix(ref string __0)
        {
            if (__0 == "999ft")
            {
                Plugin.BepInLogger.LogMessage("taking you out of the depths");
                __0 = "titlescreen";
            }
            if (Plugin.GameState == null)
                return;
            Plugin.GameState.ActiveDelivery = __0 != "titlescreen" && __0 != "results" && __0 != "scn_credits";
        }
    }

    [HarmonyPatch(typeof(Tornado), "Awake")]
    public static class Tornado_Awake
    {
        static void Postfix(Tornado __instance)
        {
            __instance.onCompleteEvent.AddListener(() => Plugin.GameState.FoundLocation(CheckId.DeliverBossFight));
        }
    }

    [HarmonyPatch(typeof(CameraManager), "MoveCamera")]
    public static class CameraManager_MoveCamera
    {
        static void Prefix(CameraManager __instance)
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
    }

    [HarmonyPatch(typeof(OS1Store), "OnPressDoneButton")]
    public static class OS1Store_OnPressDoneButton
    {
        static void Prefix()
        {
            if (Plugin.GameState.Options.LevelSegments)
                Plugin.GameState.FoundLocation(CheckId.SegmentChickenBarn2);
        }
    }

    [HarmonyPatch(typeof(TKOfficeManager), "Start")]
    public static class TKOfficeManager_Start
    {
        static void Postfix(TKOfficeManager __instance)
        {
            __instance.spotlights[2].onCompleteBTC.onPlay.AddListener(() => Plugin.GameState.FoundLocation(CheckId.SegmentTrashKingsOffice1));
            __instance.spotlights[5].onCompleteBTC.onPlay.AddListener(() => Plugin.GameState.FoundLocation(CheckId.SegmentTrashKingsOffice2));
            __instance.spotlights[8].onCompleteBTC.onPlay.AddListener(() => Plugin.GameState.FoundLocation(CheckId.SegmentTrashKingsOffice3));
        }
    }

    [HarmonyPatch(typeof(TKOfficeManager), "JailRoutine")]
    public static class TKOfficeManager_JailRoutine
    {
        static void Prefix()
        {
            Plugin.GameState.FoundLocation(CheckId.DeliverTrashKingsOffice);
        }
    }

    [HarmonyPatch(typeof(QuadcopterBigBoy), "Entrance_Enter")]
    public static class QuadcopterBigBoy_Entrance_Enter
    {
        static void Prefix()
        {
            Plugin.GameState.FoundLocation(CheckId.SegmentThe4053);
        }
    }


}
