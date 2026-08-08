using Archipelago.MultiClient.Net.Models;
using DonutCountyAP.Randomizer;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static DataManager;

namespace DonutCountyAP.Patches;

public partial class GlobalPatches
{
    static MethodInfo GetGameDataPath = AccessTools.Method(typeof(DataManager), "GetGameDataPath");
    static string GetRandomizerDataPath()
    {
        var randomizerDataPath = (string)GetGameDataPath.Invoke(null, []);
        return randomizerDataPath.Substring(0, randomizerDataPath.Length - "savegame.sav".Length) + "randomizer.xml";
    }

    [HarmonyPatch(typeof(DataManager), "InitializeSaveData_Steam"), HarmonyPostfix]
    static void DataManager_InitializeSaveData_Steam()
    {
        Plugin.BepInLogger.LogInfo("jk, killing your save data right now");
        var randomizerDataPath = GetRandomizerDataPath();
        if (FileManagement.FileExists(randomizerDataPath, false))
        {
            var xml2 = FileManagement.GetString(randomizerDataPath, string.Empty);
            Plugin.RandomizerData = SerializerHelper<RandomizerSaveData>.XmlToObject(xml2);
            Plugin.RandomizerData.Validate();
            Plugin.BepInLogger.LogDebug("found randomizer save");
        }
        else
        {
            Plugin.RandomizerData = new RandomizerSaveData();
            Plugin.BepInLogger.LogDebug("no randomizer save");
        }
        Plugin.RandomizerData.Log();
        Plugin.RandomizerData.ApplyPatches();
        DataManager.gameData = new GameSaveData()
        {
            gameComplete = 1,
            newItemsPopup = 1,
            trashopediaIndex = Plugin.RandomizerData.TrashopediaIndex,
            hasSeenGameOverCutscene = 0,
        };
    }
    [HarmonyPatch(typeof(DataManager), "SaveGameData_Steam"), HarmonyPrefix]
    static bool DataManager_SaveGameData_Steam()
    {
        Plugin.RandomizerData.TrashopediaIndex = DataManager.gameData.trashopediaIndex;
        FileManagement.SetString(GetRandomizerDataPath(), SerializerHelper<RandomizerSaveData>.ObjectToXml(Plugin.RandomizerData));
        Plugin.BepInLogger.LogInfo("not saving the game, saved ap config instead");
        // TODO: save achievement progress to slot data
        return false;
    }
}

