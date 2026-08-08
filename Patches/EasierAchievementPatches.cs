using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DonutCountyAP.Patches;

public class EasierAchievementPatches
{
    static readonly FieldInfo OS1Achievement_unlockAmount = AccessTools.Field(typeof(OS1Achievements.OS1Achievement), "unlockAmount");

    [HarmonyPatch(typeof(OS1Achievements.OS1Achievement), "UpdateAchievement"), HarmonyPrefix]
    static void OS1Achievement_UpdateAchievement1(OS1Achievements.OS1Achievement __instance)
    {
        switch(__instance.ID)
        {
            case "QUACK_100_TIMES":
                OS1Achievement_unlockAmount.SetValue(__instance, 10);
                break;
            case "BREAK_EGGS":
                OS1Achievement_unlockAmount.SetValue(__instance, 12);
                break;
            default:
                break;
        }
    }

    [HarmonyPatch(typeof(OS1Achievements.OS1Achievement), "UpdateAchievement"), HarmonyPostfix]
    static void OS1Achievement_UpdateAchievement2(OS1Achievements.OS1Achievement __instance)
    {
        switch (__instance.ID)
        {
            case "QUACK_100_TIMES":
                OS1Achievement_unlockAmount.SetValue(__instance, 100);
                break;
            case "BREAK_EGGS":
                OS1Achievement_unlockAmount.SetValue(__instance, 36);
                break;
            default:
                break;
        }
    }

    static readonly FieldInfo ClawMachineManager__secretProgress = AccessTools.Field(typeof(ClawMachineManager), "_secretProgress");
    static readonly FieldInfo ClawMachineManager__secretKey = AccessTools.Field(typeof(ClawMachineManager), "_secretKey");

    [HarmonyPatch(typeof(ClawMachineManager), "OnPressButton"), HarmonyPrefix]
    static bool ClawMachineManager_OnPressButton(ClawMachineManager __instance, ClawMachineManager.ButtonType __0)
    {
        var secretProgress = (int)ClawMachineManager__secretProgress.GetValue(__instance);
        var secretKey = (ClawMachineManager.ButtonType[])ClawMachineManager__secretKey.GetValue(__instance);
        var duplicateKey = secretProgress > 0 && __0 == secretKey[secretProgress - 1];
        return !duplicateKey;
    }

    [HarmonyPatch(typeof(PlatformAchievements), "Unlock"), HarmonyPrefix]
    static bool PlatformAchievements_Unlock()
    {
        // no cheating :)
        return false;
    }
}

