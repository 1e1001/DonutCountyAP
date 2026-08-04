using BepInEx;
using BepInEx.Logging;
using DonutCountyAP.Archipelago;
using DonutCountyAP.Randomizer;
using DonutCountyAP.Utils;
using DonutCountyAP.Patches;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using I2.Loc;
using System.Text;
using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using static DonutCountyAP.Randomizer.GameOptions;

namespace DonutCountyAP;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInProcess("DonutCounty.exe")]
public class Plugin : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "zz1e1001.DonutCountyAP";
    public const string PLUGIN_NAME = "DonutCountyAP";
    // also change version info in .csproj
    public const string PLUGIN_VERSION = "0.1.0";

    public const string MOD_DISPLAY_INFO = $"{PLUGIN_NAME} v{PLUGIN_VERSION}";
    private const string AP_DISPLAY_INFO = $"Archipelago v{ArchipelagoClient.AP_VERSION}";
    public static ManualLogSource BepInLogger;
    public static ArchipelagoClient ArchipelagoClient = null;
    public static RandomizerSaveData RandomizerData;
    public static GameState GameState = null;

    public static bool ShowTrackerGUI = false;
    public static bool ShowDebugGUI = false;

    private void Awake()
    {
        BepInLogger = Logger;
        ArchipelagoClient = new ArchipelagoClient();
        ArchipelagoConsole.Awake();
        Globals.shipping = false;
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

        ArchipelagoConsole.LogMessage($"{MOD_DISPLAY_INFO} loaded!");

    }

    private void OnGUI()
    {
        bool titlescreen = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "titlescreen";
        // TODO: this gui sucks total ass

        GUI.Label(new Rect(16, 16, 300, 20), MOD_DISPLAY_INFO);
        ArchipelagoConsole.OnGUI();

        if (titlescreen)
        {
            string statusMessage;
            if (GameState != null)
            {
                statusMessage = " Status: Connected";
                GUI.Label(new Rect(16, 50, 300, 20), AP_DISPLAY_INFO + statusMessage);
            }
            else
            {
                statusMessage = " Status: Disconnected";
                GUI.Label(new Rect(16, 50, 300, 20), AP_DISPLAY_INFO + statusMessage);
                GUI.Label(new Rect(16, 70, 150, 20), "Host: ");
                GUI.Label(new Rect(16, 90, 150, 20), "Player Name: ");
                GUI.Label(new Rect(16, 110, 150, 20), "Password: ");

                RandomizerData.Uri = GUI.TextField(new Rect(150, 70, 150, 20),
                    RandomizerData.Uri);
                RandomizerData.SlotName = GUI.TextField(new Rect(150, 90, 150, 20),
                    RandomizerData.SlotName);
                RandomizerData.Password = GUI.TextField(new Rect(150, 110, 150, 20),
                    RandomizerData.Password);

                //}
                if (GUI.Button(new Rect(16, 130, 100, 20), "Debug session"))
                {
                    SetGame(new GameState(new GameOptions()
                    {
                        GoalArea = GameOptions.GoalAreaMode.Bossfight,
                        TotalFragments = 50,
                        RequiredFragments = 40,
                        Water = true,
                        Fire = true,
                        Snake = true,
                        Light = true,
                        Bunnies = true,
                        Catapult = GameOptions.CatapultMode.Individual,
                        LevelCompletions = true,
                        LevelSegments = true,
                        Achievements = true,
                        BuyCatapult = true,
                        SnakeDanger = true,
                        SaltAndPepper = true,
                        HackProtocol = true,
                    }));
                }
            }
        }

        if (ShowDebugGUI)
        {
            GameState?.OnGUI();
        }
    }
    public static void OnTitleConnect()
    {
        if (RandomizerData.SlotName.IsNullOrWhiteSpace())
            return;
        ArchipelagoClient.Connect();
    }
    public static void OnTitleDisconnect()
    {
        ArchipelagoClient.Disconnect();
    }
    public static void OnTitleTracker()
    {
        ShowTrackerGUI ^= true;
    }
    public static void OnTitleDebug()
    {
        ShowDebugGUI ^= true;
    }
    public static void SetGame(GameState game)
    {
        Debug.Log(game == null ? "ending session" : "starting session");
        Plugin.GameState = game;
        // quit to titlescreen
        RM.sceneManager.OnQueueLevel("titlescreen");
        RM.sceneManager.OnPlayQueuedLevel();
        DataManager.SaveGameData();
    }
}