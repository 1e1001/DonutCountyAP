using DonutCountyAP.Randomizer;
using HarmonyLib;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DonutCountyAP.Patches;

public partial class GlobalPatches
{
    // TODO: visually disable play button when unavailable
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
        if (select == null || (!(bool)OS1LevelSelect__isShowing.GetValue(select) && (OS1OptionsMenu.State)GlobalPatches.OS1OptionsMenu__currentState.GetValue(RM.pauseMenu) != OS1OptionsMenu.State.Profile))
            return;
        var index = (int)OS1LevelSelect__currentDeliveryIndex.GetValue(select);
        GUI.Box(new Rect(8, 162, 316, 156), "");
        GUI.Label(new Rect(16, 170, 300, 20), $"Delivery ID {index}");
        var info = AutoLogic.LEVEL_SELECT[index];
        var unlock = info.Unlock == ItemId.None ? Plugin.GameState.UnlockedBossfight() : !Plugin.GameState.Options.Levels || Plugin.GameState.Has(info.Unlock);
        var pieces = Plugin.GameState.Quantity(ItemId.QuadcopterPiece);
        var requiredPieces = Plugin.GameState.Options.RequiredPieces[index];
        GUI.Label(new Rect(16, 190, 300, 20), $"Quadcopter Pieces: {pieces}/{requiredPieces}, Item: {unlock}");
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

            if (Plugin.Client.Locations().Contains(entry.Id))
                trackerString.Append('_');
            else
                trackerString.Append(LocationSymbol(entry.Type));
        }
        GUI.Label(new Rect(16, 210, 300, 100), trackerString.ToString());
    }

    [HarmonyPatch(typeof(OS1LevelSelect), "OnPressButtonNavigate"), HarmonyPostfix]
    static void OS1LevelSelect_OnPressButtonNavigate(OS1LevelSelect __instance)
    {
        // extra data for external trackers, done in the menu so you can quickly scroll through levels
        Plugin.Client.SetSlotData("level", (int)OS1LevelSelect__currentDeliveryIndex.GetValue(__instance));
    }

    [HarmonyPatch(typeof(OS1LevelSelect), "OnPressButtonPlay"), HarmonyPrefix]
    static bool OS1LevelSelect_OnPressButtonPlay(OS1LevelSelect __instance)
    {
        var index = (int)OS1LevelSelect__currentDeliveryIndex.GetValue(__instance);
        var info = AutoLogic.LEVEL_SELECT[index];
        var unlock = info.Unlock == ItemId.None ? Plugin.GameState.UnlockedBossfight() : !Plugin.GameState.Options.Levels || Plugin.GameState.Has(info.Unlock);
        var pieces = Plugin.GameState.Quantity(ItemId.QuadcopterPiece);
        var requiredPieces = Plugin.GameState.Options.RequiredPieces[index];
        return unlock && pieces >= requiredPieces;
    }
}

