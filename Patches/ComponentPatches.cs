using DonutCountyAP.Randomizer;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DonutCountyAP.Patches;

public partial class GlobalPatches
{
    [HarmonyPatch(typeof(OS1GameUI), "Start"), HarmonyPostfix]
    static void OS1GameUI_Start(OS1GameUI __instance)
    {
        var backflip = __instance.gameObject.AddComponent<Backflip>();
        backflip.Characters = new GameObject[__instance.characters.Length];
        for (int i = 0; i < __instance.characters.Length; ++i)
            backflip.Characters[i] = __instance.characters[i]._characterHolder;
    }
}
