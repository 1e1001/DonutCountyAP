using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace DonutCountyAP.Patches;

public class PatchTitleScreen
{
    [HarmonyPatch(typeof(OS1TitleScreen), "UpdateButtons")]
    public static class OS1TitleScreen_UpdateButtons
    {
        static void SetupButton(ButtonDeluxe button, string text, UnityAction call)
        {
            button.onRelease.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
            button.onRelease.RemoveAllListeners();
            button.onRelease.AddListener(call);
            button.gameObject.GetComponentInChildren<OS1LocalizedText>().enabled = false;
            button.gameObject.GetComponentInChildren<SuperTextMesh>().text = text;
        }

        static bool Prefix(OS1TitleScreen __instance)
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
            SetupButton(__instance.buttonLevelSelect, "Evil debug zone", Plugin.OnTitleDebug);

            return false;
        }
    }

    [HarmonyPatch(typeof(ShowTitleScreen), "Start")]
    public static class ShowTitleScreen_Start
    {
        static MethodInfo StartTitleScreenRoutine = AccessTools.Method(typeof(ShowTitleScreen), "StartTitleScreenRoutine");
        static bool Prefix(ShowTitleScreen __instance)
        {
            // TODO: beach ending when goaled
            bool complete = Plugin.GameState == null;
            bool beach = false;
            __instance.gameCompleteTitleScreen.SetActive(complete && beach);
            __instance.gameRestartTitleScreen.SetActive(complete && !beach);
            __instance.defaultTitleScreen.SetActive(!complete);
            if (complete)
            {
                if (beach)
                {
                    RM.musicManager.SetMusic(__instance.gameCompleteMusic);
                    __instance.gameCompleteEvent.SafeInvoke();
                }
                else
                {
                    RM.musicManager.SetMusic(__instance.gameRestartMusic);
                    __instance.gameRestartEvent.SafeInvoke();
                }
            }
            else
            {
                RM.musicManager.SetMusic(__instance.defaultMusic);
                __instance.defaultEvent.SafeInvoke();
            }
            RM.musicManager.StartMusic();
            __instance.StartCoroutine((System.Collections.IEnumerator)StartTitleScreenRoutine.Invoke(__instance, []));
            return false;
        }
    }
}
