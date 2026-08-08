using DonutCountyAP.Randomizer;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DonutCountyAP.Patches;
  
public partial class GlobalPatches
{
	// TODO: if i make an index of every fallstate these should be part of it
    static Dictionary<string, ItemId> CATAPULT_TYPES = new()
    {
	    ["crate"] = ItemId.CatapultBoxes,
	    ["crate1"] = ItemId.CatapultBoxes,
	    ["Chicken2"] = ItemId.CatapultChickens,
	    ["egg(Clone)"] = ItemId.CatapultEggs,
	    ["honeycombPiece"] = ItemId.CatapultHoneycomb,
	    ["Frog"] = ItemId.CatapultFrogs,
	    ["WaterBalloon"] = ItemId.CatapultWater,
	    ["WaterBalloon(Clone)"] = ItemId.CatapultWater,
	    ["fish"] = ItemId.CatapultWater,
	    ["donutHero"] = ItemId.CatapultDonutsCameras,
	    ["polaroid"] = ItemId.CatapultDonutsCameras,
	    ["RaccoonCop"] = ItemId.CatapultRaccoons,
	    ["RaccoonBigBoy"] = ItemId.CatapultRaccoons,
	    ["RaccoonSleeper (hq)"] = ItemId.CatapultRaccoons,
	    ["hackerDevice"] = ItemId.CatapultHackingDevice,
	    ["hackerDevice (1)"] = ItemId.CatapultHackingDevice,
	    ["keycardBio"] = ItemId.CatapultKeycards,
	    ["keycard Anthro"] = ItemId.CatapultKeycards,
	    ["RaccoonBomb(Clone)"] = ItemId.CatapultBombs,
    };

    static readonly FieldInfo HoleContents_contents = AccessTools.Field(typeof(HoleContents), "contents");
    [HarmonyPatch(typeof(HoleContents), "Remove"), HarmonyPrefix]
    static bool HoleContents_Remove(HoleContents __instance, bool __1)
    {
        // bypass
        if (__1)
            return true;
        if (__instance.GetWater())
            return Plugin.GameState.HasCatapult(ItemId.CatapultWater);
        var contents = (List<GameObject>)HoleContents_contents.GetValue(__instance);
        if (contents.Count == 0)
            return true;
        GameObject top = contents[contents.Count - 1];
		if (!CATAPULT_TYPES.TryGetValue(top.name, out var item)) {
			Plugin.BepInLogger.LogWarning($"launching mysterious object {top}");
			return true;
		}
		if (Plugin.GameState.HasCatapult(item))
			return true;
		Plugin.BepInLogger.LogInfo($"prevening launch of {top} as player does not have {item}");
		return false;
    }
}
