using BepInEx;
using BepInEx.Logging;
using DonutCountyAP.Archipelago;
using DonutCountyAP.Randomizer;
using DonutCountyAP.Patches;
using UnityEngine;
using DonutCountyAP.Generated;

namespace DonutCountyAP;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, VersionInfo.VERSION), BepInProcess("DonutCounty.exe")]
public class Plugin : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "zz1e1001.DonutCountyAP";
    public const string PLUGIN_NAME = "DonutCountyAP";

    public const string MOD_DISPLAY_INFO = $"{PLUGIN_NAME} {VersionInfo.TEXT_VERSION}";
    public static ManualLogSource BepInLogger;
    // TODO: Client and GameState are extremely coupled together (via the global Plugin)
    // particularly there's some weird handling about when each is null or not
    // not a problem yet but it should be looked into more
    public static IRandomizerClient Client = null;
    public static GameState GameState = null;
    public static ClientOptions Options = null;
    public static Patcher Patcher = new();
    public static Logic Logic;

    public static bool ShowOptionsGUI = false;
    public static bool ShowDebugGUI = false;

    void Awake()
    {
        BepInLogger = Logger;
        Logic = Logic.LoadEmbedded();
        PackedAssets.LoadEmbedded();
        ArchipelagoConsole.Awake();
        Globals.shipping = false;
        Patcher.Global.Set(true);
        Plugin.BepInLogger.LogMessage($"{MOD_DISPLAY_INFO} loaded!");
    }

    string GUIStatus()
    {
        if (Client == null)
            return ArchipelagoClient.AP_DEFAULT_STATUS;
        return Client.GUIStatus() + (Client.Connecting() ? " Connecting..." : " Connected");
    }

    void OnGUI()
    {
        bool titlescreen = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "titlescreen";
        // TODO: this gui sucks total ass

        GUI.Label(new Rect(16, 16, 300, 20), MOD_DISPLAY_INFO);
        ArchipelagoConsole.OnGUI();

        if (titlescreen)
        {
            GUI.Label(new Rect(16, 50, 300, 20), GUIStatus());
            if (GameState == null)
            {
                GUI.Label(new Rect(16, 70, 150, 20), "Host: ");
                GUI.Label(new Rect(16, 90, 150, 20), "Player Name: ");
                GUI.Label(new Rect(16, 110, 150, 20), "Password: ");

                Options.Uri = GUI.TextField(new Rect(150, 70, 150, 20),
                    Options.Uri);
                Options.SlotName = GUI.TextField(new Rect(150, 90, 150, 20),
                    Options.SlotName);
                Options.Password = GUI.TextField(new Rect(150, 110, 150, 20),
                    Options.Password);

            }

        }
        //if (ShowOptionsGUI)
        if (RM.pauseMenu != null && (OS1OptionsMenu.State)GlobalPatches.OS1OptionsMenu__currentState.GetValue(RM.pauseMenu) == OS1OptionsMenu.State.Options)
            Options.OnGUI();

        if (ShowDebugGUI)
            GameState?.OnGUI();

        if (GameState != null)
            GlobalPatches.LevelSelectGUI();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            if (GameState != null)
            {
                ShowDebugGUI ^= true;
            }
            else
            {
                Client = new DebugClient();
                SetGame(new GameState(new GameOptions()));
            }
        }
        Client?.Update();
    }
    public static void OnTitleConnect()
    {
        if (Options.SlotName.IsNullOrWhiteSpace())
            return;
        if (Client?.Connecting() ?? false)
            return;
        Client = new ArchipelagoClient();
    }
    public static void OnTitleDisconnect()
    {
        RM.os1popup.StartPopup("Disconnect?", "Menus/QUIT_CONFIRM", "Menus/OKAY", "Menus/NO", delegate
        {
            Client.Disconnect();
        }, null);
    }
    public static void OnTitleOptions()
    {
        ShowOptionsGUI ^= true;
    }
    public static void SetGame(GameState game)
    {
        if (game == null)
            Client = null;
        if (game == GameState)
            return;
        Debug.Log(game == null ? "ending session" : "starting session");
        GameState = game;
        if (GameState == null)
            GameOptions.UnpatchAll();
        // quit to titlescreen
        RM.sceneManager.OnQueueLevel("titlescreen");
        RM.sceneManager.OnPlayQueuedLevel();
        DataManager.SaveGameData();
    }
}
