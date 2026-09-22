using DonutCountyAP.Generated;
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

    static char LocationSymbol(Logic.LocationType type)
    {
        switch (type)
        {
            case Logic.LocationType.Delivery:
                return 'C';
            case Logic.LocationType.Segment:
                return 'S';
            case Logic.LocationType.Achievement:
                return 'A';
            case Logic.LocationType.SnakeDanger:
            case Logic.LocationType.Catapult:
            case Logic.LocationType.SaltAndPepper:
                return 'G';
            case Logic.LocationType.Victory:
                return 'V';
            default:
                return '?';
        }
    }
    static bool ShowLevelSelect(OS1LevelSelect select)
    {
        var pause = (OS1OptionsMenu.State)GlobalPatches.OS1OptionsMenu__currentState.GetValue(RM.pauseMenu);
        if ((bool)OS1LevelSelect__isShowing.GetValue(select) && pause == OS1OptionsMenu.State.Closed)
            return true;
        if (pause == OS1OptionsMenu.State.Profile)
            return true;
        return false;
    }

    public static void LevelSelectGUI()
    {
        OS1LevelSelect select = RM.os1LevelSelect;
        if (select == null || Plugin.GameState == null)
            return;
        if (!ShowLevelSelect(select))
            return;
        var index = (int)OS1LevelSelect__currentDeliveryIndex.GetValue(select);
        GUI.Box(new Rect(8, 162, 316, 156), "");
        GUI.Label(new Rect(16, 170, 300, 20), $"Delivery ID {index}");
        var info = Plugin.Logic.Levels[index];
        var unlock = info.Unlock == ItemId.None ? Plugin.GameState.UnlockedBossfight() : !Plugin.GameState.Options.Levels || Plugin.GameState.Has(info.Unlock);
        var pieces = Plugin.GameState.Quantity(ItemId.QuadcopterPiece);
        var requiredPieces = Plugin.GameState.Options.RequiredPieces[index];
        GUI.Label(new Rect(16, 190, 300, 20), $"Quadcopter Pieces: {pieces}/{requiredPieces}, Item: {unlock}");
        var trackerString = new StringBuilder();
        foreach (var segment in info.Segments)
        {
            var previousLine = true;
            var previousType = Logic.LocationType.Victory;
            switch (Plugin.GameState.Options.Trashsanity)
            {
                case GameOptions.TrashsanityMode.Off:
                    break;
                case GameOptions.TrashsanityMode.Types:
                    trackerString.Append($"{Plugin.GameState.Counter(segment.Types)}/{Plugin.Logic.Counters[segment.Types]}");
                    previousLine = false;
                    break;
                case GameOptions.TrashsanityMode.All:
                    trackerString.Append($"{Plugin.GameState.Counter(segment.Trash)}/{Plugin.Logic.Counters[segment.Trash]}");
                    previousLine = false;
                    break;
            }

            foreach (var entry in segment.Locations)
            {
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

            trackerString.Append('\n');
        }
        GUI.Label(new Rect(16, 210, 300, 100), trackerString.ToString());
    }

    [HarmonyPatch(typeof(OS1LevelSelect), "SetLevel"), HarmonyPostfix]
    static void OS1LevelSelect_SetLevel(OS1LevelSelect __instance)
    {
        Plugin.Client?.SetSlotStorage("level", $"select:{(int)OS1LevelSelect__currentDeliveryIndex.GetValue(__instance)}");
    }

    [HarmonyPatch(typeof(OS1LevelSelect), "OnPressButtonPlay"), HarmonyPrefix]
    static bool OS1LevelSelect_OnPressButtonPlay(OS1LevelSelect __instance)
    {
        var index = (int)OS1LevelSelect__currentDeliveryIndex.GetValue(__instance);
        var info = Plugin.Logic.Levels[index];
        var unlock = info.Unlock == ItemId.None ? Plugin.GameState.UnlockedBossfight() : !Plugin.GameState.Options.Levels || Plugin.GameState.Has(info.Unlock);
        var pieces = Plugin.GameState.Quantity(ItemId.QuadcopterPiece);
        var requiredPieces = Plugin.GameState.Options.RequiredPieces[index];
        var proceed = unlock && pieces >= requiredPieces;
        if (proceed)
            Plugin.Client?.SetSlotStorage("level", $"game:{index}");
        return proceed;
    }

    [HarmonyPatch(typeof(OS1LevelSelect), "OnPressButtonBack"), HarmonyPrefix]
    static void OS1LevelSelect_OnPressButtonBack(OS1LevelSelect __instance)
    {
        // this is called from titlescreen
        if (__instance == null)
            return;
        Plugin.Client?.SetSlotStorage("level", $"title:{(int)OS1LevelSelect__currentDeliveryIndex.GetValue(__instance)}");
    }

    [HarmonyPatch(typeof(OS1OptionsMenu), "OnPressDebugRestartLevel"), HarmonyPrefix]
    static bool OS1OptionsMenu_OnPressDebugRestartLevel(OS1OptionsMenu __instance)
    {
        if (OS1LevelSelect_OnPressButtonPlay(RM.os1LevelSelect))
        {
            OS1Delivery deliveryDataLevelSelect = DataManager.GetDeliveryDataLevelSelect((int)OS1LevelSelect__currentDeliveryIndex.GetValue(RM.os1LevelSelect));
            DataManager.SetCurrentDelivery(deliveryDataLevelSelect);
            UnityEngine.SceneManagement.SceneManager.LoadScene(deliveryDataLevelSelect.scene);
            __instance.PauseGame();
        }
        return false;
    }
    [HarmonyPatch(typeof(OS1OptionsMenu), "SetState"), HarmonyPostfix]
    static void OS1OptionsMenu_SetState(OS1OptionsMenu __instance)
    {
        __instance.restartLevelButton.SetButtonActive(true, false);
    }
}

