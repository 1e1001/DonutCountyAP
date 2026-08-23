using DonutCountyAP.Randomizer;
using HarmonyLib;

namespace DonutCountyAP.Patches;

public partial class GlobalPatches
{
    // not to be confused with DialogueSkippingPatches

    [HarmonyPatch(typeof(TextingManager), "OnPressContinue"), HarmonyPrefix]
    static bool TextingManager_OnPressContinue()
    {
        return !Plugin.GameState.Options.Texting || Plugin.GameState.Has(ItemId.Texting);
    }

    [HarmonyPatch(typeof(TextingManager), "OnPressQuack"), HarmonyPrefix]
    static bool TextingManager_OnPressQuack()
    {
        return !Plugin.GameState.Options.Texting || Plugin.GameState.Has(ItemId.Texting);
    }
}
