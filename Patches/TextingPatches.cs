using DonutCountyAP.Randomizer;
using HarmonyLib;

namespace DonutCountyAP.Patches;

public partial class GlobalPatches
{
    // not to be confused with DialogueSkippingPatches

    [HarmonyPatch(typeof(TextingManager), "OnPressContinue"), HarmonyPrefix]
    static bool TextingManager_OnPressContinue()
    {
        // this doesn't prevent quacking, which is arguably funnier imo
        return !Plugin.GameState.Options.Texting || Plugin.GameState.Has(ItemId.Texting);
    }
}
