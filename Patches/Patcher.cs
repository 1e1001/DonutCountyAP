using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DonutCountyAP.Patches;

public class Patcher
{
    public PatchSet Global = new(typeof(GlobalPatches));
    public PatchSet EasierAchievements = new(typeof(EasierAchievementPatches));
    public PatchSet DialogueSkipping = new(typeof(DialogueSkippingPatches));
    public PatchSet SnakeDanger = new(typeof(SnakeDangerPatches));
    public PatchSet SaltAndPepper = new(typeof(SaltAndPepperPatches));
    public PatchSet HackProtocol = new(typeof(HackProtocolPatches));

    // for some reason record doesn't work here?
    public class PatchSet(Type Type)
    {
        public bool Enabled = false;
        public Harmony Patcher = new($"{Plugin.PLUGIN_GUID}.{Type.Name}");

        public void Set(bool enabled)
        {
            if (enabled == Enabled)
                return;
            Plugin.BepInLogger.LogDebug($"repatch {Type.Name}={enabled}");
            if (Enabled = enabled)
                Patcher.PatchAll(Type);
            else
                Patcher.UnpatchSelf();
        }
    }

}
