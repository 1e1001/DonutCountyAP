using DonutCountyAP.Randomizer;
using HarmonyLib;

namespace DonutCountyAP.Patches;

public partial class GlobalPatches
{
    static readonly bool[] DELIVERY_ENDS_LEVEL = [
        false,
        true, // bk texting cutscene
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        true, // donut shop
        true,
        true,
        true,
        true, // above donut county
        false,
        true, // hq entrance interior
        false,
        true, // path to anthropology
        false,
        true, // path to tk office
        true,
        true, // bossfight
        false, // catapult
        true, // aftermath
        false, // game over (continue to bossfight)
    ];

    [HarmonyPatch(typeof(SceneManager), "OnLevelComplete"), HarmonyPrefix]
    static void SceneManager_OnLevelComplete()
    {
        var delivery = DataManager.GetCurrentDelivery();
        if (DELIVERY_ENDS_LEVEL[delivery])
        {
            // a little hacky to change the nextScene on the delivery directly, but it's right before the only time that's referenced
            if (delivery == 25 && Plugin.GameState.Options.GoalArea == GameOptions.GoalAreaMode.Bossfight)
            {
                DataManager.GetCurrentDeliveryData().nextScene = "999ft";
            } else
            {
                DataManager.GetCurrentDeliveryData().nextScene = "titlescreen";
                Plugin.GameState.ActiveDelivery = false;
            }
        }
        Plugin.GameState.FoundEvent($"delivery{delivery}", true);
    }

    [HarmonyPatch(typeof(SceneManager), "OnQueueLevel"), HarmonyPrefix]
    static void SceneManager_OnQueueLevel(ref string level)
    {
        if (level == "999ft" || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "999ft")
        {
            Plugin.BepInLogger.LogInfo("saving you from the depths");
            level = "titlescreen";
        }
        if (level == "999ft_forced")
            level = "999ft";
        if (Plugin.GameState == null)
            return;
        Plugin.GameState.ActiveDelivery = level != "titlescreen" && level != "results" && level != "scn_credits" && level != "999ft";
    }

    [HarmonyPatch(typeof(CameraManager), "MoveCamera"), HarmonyPrefix]
    static void CameraManager_MoveCamera(CameraManager __instance)
    {
        // this is called outside of gameplay
        if (Plugin.GameState == null || !Plugin.GameState.ActiveDelivery)
            return;
        var delivery = DataManager.GetCurrentDelivery();
        // in deliveries, index is always the next camera
        var camera = __instance.GetIndex();
        var eventId = $"delivery{delivery}camera{camera}";
        Plugin.GameState.FoundEvent(eventId, true);
    }
}
