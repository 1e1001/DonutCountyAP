using HarmonyLib;

namespace DonutCountyAP.Patches;

public partial class GlobalPatches
{

    [HarmonyPatch(typeof(OS1Store), "BuyItem"), HarmonyPrefix]
    static void OS1Store_BuyItem(OS1Store __instance, OS1ItemUI __0)
    {
        if (__0.item == __instance.mustBuyToExit)
            Plugin.GameState.FoundEvent("store_catapult");
    }

    [HarmonyPatch(typeof(OS1Store), "OnPressDoneButton"), HarmonyPrefix]
    static void OS1Store_OnPressDoneButton()
    {
        Plugin.GameState.FoundEvent("store_done");
    }

    [HarmonyPatch(typeof(QuadcopterBigBoy), "Entrance_Enter"), HarmonyPrefix]
    static void QuadcopterBigBoy_Entrance_Enter()
    {
        Plugin.GameState.FoundEvent("quadcopter_big_boy");
    }

    [HarmonyPatch(typeof(HQAnthropologyManager), "OnFirstRocketHitVent"), HarmonyPrefix]
    static void HQAnthropologyManager_OnFirstRocketHitVent()
    {
        Plugin.GameState.FoundEvent("anthropology_vent");
    }

    [HarmonyPatch(typeof(HQAnthropologyManager), "Start"), HarmonyPrefix]
    static void HQAnthropologyManager_Start(HQAnthropologyManager __instance)
    {
        __instance.ventTrigger.onVentHit.AddListener(() => Plugin.GameState.FoundEvent("anthropology_end"));
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

    [HarmonyPatch(typeof(BossFight), "Upgrade_Enter"), HarmonyPrefix]
    static void BossFight_Upgrade_Enter()
    {
        Plugin.GameState.FoundEvent("boss_upgrade");
    }

    [HarmonyPatch(typeof(Tornado), "Awake"), HarmonyPostfix]
    static void Tornado_Awake(Tornado __instance)
    {
        __instance.onCompleteEvent.AddListener(() => Plugin.GameState.FoundEvent("boss_tornado"));
    }

    [HarmonyPatch(typeof(OS1Achievements.OS1Achievement), "UnlockAchievement"), HarmonyPrefix]
    static void OS1Achievement_UnlockAchievement(OS1Achievements.OS1Achievement __instance)
    {
        Plugin.GameState.FoundEvent(__instance.ID);
    }
}

