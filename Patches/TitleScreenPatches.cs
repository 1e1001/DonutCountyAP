using HarmonyLib;
using System.Reflection;
using UnityEngine.Events;

namespace DonutCountyAP.Patches;

public partial class GlobalPatches
{
    static void SetupButton(ButtonDeluxe button, string text, UnityAction call)
    {
        button.onRelease.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
        button.onRelease.RemoveAllListeners();
        button.onRelease.AddListener(call);
        button.gameObject.GetComponentInChildren<OS1LocalizedText>().enabled = false;
        button.gameObject.GetComponentInChildren<SuperTextMesh>().text = text;
    }

    [HarmonyPatch(typeof(OS1TitleScreen), "UpdateButtons"), HarmonyPrefix]
    static bool OS1TitleScreen_UpdateButtons(OS1TitleScreen __instance)
    {
        bool inGame = Plugin.GameState != null;
        __instance.buttonQuit.gameObject.SetActive(true);
        __instance.buttonNewGame.gameObject.SetActive(true);
        __instance.buttonOptions.gameObject.SetActive(true);
        __instance.buttonContinue.gameObject.SetActive(inGame);
        __instance.buttonLevelSelect.gameObject.SetActive(inGame);
        __instance.buttonCredits.gameObject.SetActive(inGame);
        __instance.buttonTrashopedia.gameObject.SetActive(inGame);
        __instance.rightSideHolder.SetActive(inGame);
        // setup buttons
        SetupButton(__instance.buttonContinue, "Levels", __instance.OnPressButtonLevelSelect);
        if (inGame)
            SetupButton(__instance.buttonNewGame, "Disconnect", Plugin.OnTitleDisconnect);
        else
            SetupButton(__instance.buttonNewGame, "Connect", Plugin.OnTitleConnect);
        SetupButton(__instance.buttonLevelSelect, "???", Plugin.OnTitleOptions);

        return false;
    }

    static readonly MethodInfo ShowTitleScreen_StartTitleScreenRoutine = AccessTools.Method(typeof(ShowTitleScreen), "StartTitleScreenRoutine");

    [HarmonyPatch(typeof(ShowTitleScreen), "Start"), HarmonyPrefix]
    static bool Prefix(ShowTitleScreen __instance)
    {
        var complete = Plugin.GameState == null;
        var beach = Plugin.Client?.IsComplete() ?? false;
        complete |= beach;
        Plugin.BepInLogger.LogDebug($"Titlescreen {complete}+{beach}");
        __instance.gameCompleteTitleScreen.SetActive(complete && beach);
        __instance.gameRestartTitleScreen.SetActive(complete && !beach);
        __instance.defaultTitleScreen.SetActive(!complete);
        if (beach)
        {
            RM.musicManager.SetMusic(__instance.gameCompleteMusic);
            __instance.gameCompleteEvent.SafeInvoke();
        }
        else if (complete)
        {
            RM.musicManager.SetMusic(__instance.gameRestartMusic);
            __instance.gameRestartEvent.SafeInvoke();
        }
        else
        {
            RM.musicManager.SetMusic(__instance.defaultMusic);
            __instance.defaultEvent.SafeInvoke();
        }
        RM.musicManager.StartMusic();
        __instance.StartCoroutine((System.Collections.IEnumerator)ShowTitleScreen_StartTitleScreenRoutine.Invoke(__instance, []));
        return false;
    }


    public static readonly FieldInfo OS1OptionsMenu__currentState = AccessTools.Field(typeof(OS1OptionsMenu), "_currentState");
}
