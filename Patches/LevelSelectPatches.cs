using DonutCountyAP.Randomizer;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DonutCountyAP.Patches;

public partial class GlobalPatches
{
    [HarmonyPatch(typeof(DataManager), "GetDeliveryListLevelSelect"), HarmonyPostfix]
    static void DataManager_GetDeliveryListLevelSelect(ref List<OS1Delivery> __result)
    {
        // result is cached between calls, so check that it's the first run
        if (__result.Count != 24)
            return;
        __result.RemoveAt(22);
        __result.RemoveAt(16);
    }

    // TODO: visually disable play button
    static readonly FieldInfo OS1LevelSelect__isShowing = AccessTools.Field(typeof(OS1LevelSelect), "_isShowing");
    static readonly FieldInfo OS1LevelSelect__currentDeliveryIndex = AccessTools.Field(typeof(OS1LevelSelect), "_currentDeliveryIndex");

    static char LocationSymbol(AutoLogic.LocationType type)
    {
        switch (type)
        {
            case AutoLogic.LocationType.Delivery:
                return 'C';
            case AutoLogic.LocationType.Segment:
                return 'S';
            case AutoLogic.LocationType.Achievement:
                return 'A';
            case AutoLogic.LocationType.SnakeDanger:
            case AutoLogic.LocationType.Catapult:
            case AutoLogic.LocationType.SaltAndPepper:
            case AutoLogic.LocationType.HackProtocol:
                return 'G';
            case AutoLogic.LocationType.Victory:
                return 'V';
            default:
                return '?';
        }
    }

    public static void LevelSelectGUI()
    {
        OS1LevelSelect select = RM.os1LevelSelect;
        if (select == null || !(bool)OS1LevelSelect__isShowing.GetValue(select))
            return;
        var index = (int)OS1LevelSelect__currentDeliveryIndex.GetValue(select);
        GUI.Box(new Rect(8, 162, 316, 156), "");
        GUI.Label(new Rect(16, 170, 300, 20), $"Delivery ID {index}");
        var info = AutoLogic.LEVEL_SELECT[index];
        var unlock = info.Unlock == ItemId.None ? Plugin.GameState.UnlockedBossfight() : !Plugin.GameState.Options.Levels || Plugin.GameState.Has(info.Unlock);
        var fragments = Plugin.GameState.Quantity(ItemId.Fragment);
        var requiredFragments = Plugin.GameState.Options.RequiredFragments[index];
        GUI.Label(new Rect(16, 190, 300, 20), $"Fragments: {fragments}/{requiredFragments}, Item: {unlock}");
        var trackerString = new StringBuilder();
        var previousType = AutoLogic.LocationType.Victory;
        var previousLine = true;
        foreach (var entry in info.Locations)
        {
            if (entry.Id == -1)
            {
                trackerString.Append('\n');
                previousLine = true;
                continue;
            }
            if (!Plugin.GameState.Options.CanSendLocation(entry.Type))
                continue;
            if (!previousLine && entry.Type != previousType)
                trackerString.Append(' ');
            previousType = entry.Type;
            previousLine = false;

            if (Plugin.GameState.HasLocation(entry.Id))
                trackerString.Append('_');
            else
                trackerString.Append(LocationSymbol(entry.Type));
        }
        GUI.Label(new Rect(16, 210, 300, 100), trackerString.ToString());
    }


    [HarmonyPatch(typeof(OS1LevelSelect), "OnPressButtonPlay"), HarmonyPrefix]
    static bool OS1LevelSelect_OnPressButtonPlay(OS1LevelSelect __instance)
    {
        var index = (int)OS1LevelSelect__currentDeliveryIndex.GetValue(__instance);
        var info = AutoLogic.LEVEL_SELECT[index];
        var unlock = info.Unlock == ItemId.None ? Plugin.GameState.UnlockedBossfight() : !Plugin.GameState.Options.Levels || Plugin.GameState.Has(info.Unlock);
        var fragments = Plugin.GameState.Quantity(ItemId.Fragment);
        var requiredFragments = Plugin.GameState.Options.RequiredFragments[index];
        return unlock && fragments >= requiredFragments;
    }
}

