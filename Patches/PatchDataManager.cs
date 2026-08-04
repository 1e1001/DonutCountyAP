using Archipelago.MultiClient.Net.Models;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static DataManager;

namespace DonutCountyAP.Patches;

public partial class PatchDataManager
{
    static MethodInfo GetGameDataPath = AccessTools.Method(typeof(DataManager), "GetGameDataPath");
    static string GetRandomizerDataPath()
    {
        var randomizerDataPath = (string)GetGameDataPath.Invoke(null, []);
        var place = randomizerDataPath.LastIndexOf("savegame");
        return randomizerDataPath.Remove(place, "savegame".Length).Insert(place, "randomizer");
    }

    [HarmonyPatch(typeof(DataManager), "InitializeSaveData_Steam")]
    public static class DataManager_InitializeSaveData_Steam
    {
        static void Postfix()
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
            DataManager.gameData = new GameSaveData()
            {
                gameComplete = 1,
                newItemsPopup = 1,
                trashopediaIndex = Plugin.RandomizerData.TrashopediaIndex,
            };
        }
    }
    [HarmonyPatch(typeof(DataManager), "SaveGameData_Steam")]
    public static class DataManager_SaveGameData_Steam
    {
        static bool Prefix()
        {
            Plugin.RandomizerData.TrashopediaIndex = DataManager.gameData.trashopediaIndex;
            FileManagement.SetString(GetRandomizerDataPath(), SerializerHelper<RandomizerSaveData>.ObjectToXml(Plugin.RandomizerData));
            Plugin.BepInLogger.LogInfo("not saving the game, saved ap config instead");
            return false;
        }
    }
}

