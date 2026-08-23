using HarmonyLib;
using UnityEngine;

namespace DonutCountyAP.Patches;

public class DialogueSkippingPatches
{
    static float _timer = 0;

    [HarmonyPatch(typeof(ButtonTriggerEvent), "Hold"), HarmonyPrefix]
    static void ButtonTriggerEvent_Hold(ButtonTriggerEvent __instance)
    {
        if (__instance.gameObject.name != "DialogNextButton")
            return;
        _timer += Time.deltaTime;
        if (_timer > 0.5f)
            __instance.Do();
    }
    // TODO: have this but without it resetting itself
    //[HarmonyPatch(typeof(ButtonTriggerEvent), "Do"), HarmonyPrefix]
    //static void ButtonTriggerEvent_Do(ButtonTriggerEvent __instance)
    //{
    //    if (__instance.gameObject.name != "DialogNextButton")
    //        return;
    //    _timer = 0;
    //}
    [HarmonyPatch(typeof(ButtonTriggerEvent), "Up"), HarmonyPrefix]
    static void ButtonTriggerEvent_Up(ButtonTriggerEvent __instance)
    {
        if (__instance.gameObject.name != "DialogNextButton")
            return;
        _timer = 0;
    }

    [HarmonyPatch(typeof(TextingManager), "SetCloseButton"), HarmonyPrefix]
    static void TextingManager_SetCloseButton(ref bool showCloseButton)
    {
        showCloseButton |= !Plugin.GameState.Options.Texting || Plugin.GameState.Has(Randomizer.ItemId.Texting);
    }
}
