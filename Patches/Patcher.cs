using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DonutCountyAP.Patches;

public class Patcher
{
    public PatchData Global = new(typeof(GlobalPatches));
    public PatchData HoleWater = new(typeof(HoleWaterPatches));

    public record PatchData(Type Type)
    {
        public bool Enabled = true;
        public Harmony Patcher = new($"{Plugin.PLUGIN_GUID}.{Type.Name}");

        public void Set(bool enabled)
        {
            if (enabled == Enabled)
                return;
            Plugin.BepInLogger.LogDebug($"repatch {Type.Name}={enabled}");
            if (enabled)
                Patcher.PatchAll(Type);
            else
                Patcher.UnpatchSelf();
        }
    }

}
