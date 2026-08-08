using DonutCountyAP.Randomizer;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DonutCountyAP.Patches;

public partial class GlobalPatches
{

    [HarmonyPatch(typeof(OS1Store), "BuyItem"), HarmonyPrefix]
    static void OS1Store_BuyItem(OS1Store __instance, OS1ItemUI __0)
    {
        if (__0.item == __instance.mustBuyToExit)
            Plugin.GameState.FoundEvent("store_catapult");
    }

    [HarmonyPatch(typeof(BossFight), "Upgrade_Enter"), HarmonyPrefix]
    static void BossFight_Upgrade_Enter()
    {
        Plugin.GameState.FoundEvent("boss_upgrade");
    }

    [HarmonyPatch(typeof(HQAnthropologyManager), "OnFirstRocketHitVent"), HarmonyPrefix]
    static void HQAnthropologyManager_OnFirstRocketHitVent()
    {
        Plugin.GameState.FoundEvent("anthropology_vent");
    }

    [HarmonyPatch(typeof(OS1Achievements.OS1Achievement), "UpdateAchievement"), HarmonyPostfix]
    static void OS1Achievement_UpdateAchievement(OS1Achievements.OS1Achievement __instance)
    {
        // TODO: sync slotdata
    }
    [HarmonyPatch(typeof(OS1Achievements.OS1Achievement), "UnlockAchievement"), HarmonyPrefix]
    static void OS1Achievement_UnlockAchievement(OS1Achievements.OS1Achievement __instance)
    {
        Plugin.GameState.FoundEvent(__instance.ID);
    }
}

