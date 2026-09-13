using DonutCountyAP.Randomizer;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace DonutCountyAP.Patches;

public partial class GlobalPatches
{
    static MethodInfo DataManager_GetGameDataPath = AccessTools.Method(typeof(DataManager), "GetGameDataPath");
    static string GetRandomizerDataPath()
    {
        var randomizerDataPath = (string)DataManager_GetGameDataPath.Invoke(null, []);
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
            Plugin.Options = SerializerHelper<ClientOptions>.XmlToObject(xml2);
            Plugin.Options.Validate();
            Plugin.BepInLogger.LogDebug("found randomizer save");
        }
        else
        {
            Plugin.Options = new ClientOptions();
            Plugin.BepInLogger.LogDebug("no randomizer save");
        }
        Plugin.Options.Log();
        Plugin.Options.ApplyPatches();
        DataManager.gameData = new DataManager.GameSaveData()
        {
            gameComplete = 1,
            newItemsPopup = 1,
            trashopediaIndex = Plugin.Options.TrashopediaIndex,
            hasSeenGameOverCutscene = 0,
        };
    }
    [HarmonyPatch(typeof(DataManager), "SaveGameData_Steam"), HarmonyPrefix]
    static bool DataManager_SaveGameData_Steam()
    {
        Plugin.Options.TrashopediaIndex = DataManager.gameData.trashopediaIndex;
        // TODO: delay & debounce saving, then call it more often
        // also check cache lock!
        FileManagement.SetString(GetRandomizerDataPath(), SerializerHelper<ClientOptions>.ObjectToXml(Plugin.Options));
        Plugin.BepInLogger.LogInfo("not saving the game, saved ap config instead");
        //if (Plugin.Client != null)
        //{
        //    Plugin.Client.SetSlotDataMax("RELEASE_HOT_AIR_BALLOON", DataManager.gameData.RELEASE_HOT_AIR_BALLOON);
        //    Plugin.Client.SetSlotDataMax("DESTROY_DONUT_SHOP", DataManager.gameData.DESTROY_DONUT_SHOP);
        //    Plugin.Client.SetSlotDataMax("DESTROY_RACCOON_LAGOON", DataManager.gameData.DESTROY_RACCOON_LAGOON);
        //    Plugin.Client.SetSlotDataMax("HACK_HQ", DataManager.gameData.HACK_HQ);
        //    Plugin.Client.SetSlotDataMax("WIN_BOSS_FIGHT", DataManager.gameData.WIN_BOSS_FIGHT);
        //    Plugin.Client.SetSlotDataMax("CATAPULT_COMPLETE", DataManager.gameData.CATAPULT_COMPLETE);
        //    Plugin.Client.SetSlotDataMax("COMPLETE_TRASHOPEDIA", DataManager.gameData.COMPLETE_TRASHOPEDIA);
        //    Plugin.Client.SetSlotDataMax("BUY_GAMER_FUEL", DataManager.gameData.BUY_GAMER_FUEL);
        //    Plugin.Client.SetSlotDataMax("SET_TRAILER_ON_FIRE", DataManager.gameData.SET_TRAILER_ON_FIRE);
        //    Plugin.Client.SetSlotDataMax("QUACK_100_TIMES", DataManager.gameData.QUACK_100_TIMES);
        //    Plugin.Client.SetSlotDataMax("BREAK_EGGS", DataManager.gameData.BREAK_EGGS);
        //    Plugin.Client.SetSlotDataMax("COLLECT_RADIO_LAST", DataManager.gameData.COLLECT_RADIO_LAST);
        //    Plugin.Client.SetSlotDataMax("FLAWLESS_BOSS_FIGHT", DataManager.gameData.FLAWLESS_BOSS_FIGHT);
        //    Plugin.Client.SetSlotDataMax("LOSE_BOSS_FIGHT", DataManager.gameData.LOSE_BOSS_FIGHT);
        //    Plugin.Client.SetSlotDataMax("DESTROY_MONITOR", DataManager.gameData.DESTROY_MONITOR);
        //    Plugin.Client.SetSlotDataMax("DESTROY_MONUMENT", DataManager.gameData.DESTROY_MONUMENT);
        //    Plugin.Client.SetSlotDataMax("MAKE_SECRET_SOUP", DataManager.gameData.MAKE_SECRET_SOUP);
        //    Plugin.Client.SetSlotDataMax("FLY_THROUGH_DONUT_HOLE", DataManager.gameData.FLY_THROUGH_DONUT_HOLE);
        //    Plugin.Client.SetSlotDataMax("FIND_AIRSHIP", DataManager.gameData.FIND_AIRSHIP);
        //    Plugin.Client.SetSlotDataMax("UNLOCK_HQ_VAULT", DataManager.gameData.UNLOCK_HQ_VAULT);
        //}
        return false;
    }

    [HarmonyPatch(typeof(DataManager), "GetDeliveryListLevelSelect"), HarmonyPostfix]
    static void DataManager_GetDeliveryListLevelSelect(ref List<OS1Delivery> __result)
    {
        // result is cached between calls, so check that it's the first run
        if (__result.Count != 24)
            return;
        __result.RemoveAt(22);
        __result.RemoveAt(16);
    }
}

