using System.Reflection;
using UnityEngine;

namespace DonutCountyAP.Randomizer;

public class PackedAssets
{
    public static GameObject Marker;
    public static Material TrashLocked;

    public static void LoadEmbedded()
    {
        // TODO: this is the same code as Logic, unify them
        var asm = Assembly.GetExecutingAssembly();
        var fullPath = $"{asm.GetName().Name}.Generated.assets.unityfs";
        using var stream = asm.GetManifestResourceStream(fullPath);
        var bundle = AssetBundle.LoadFromStream(stream);
        Marker = bundle.LoadAsset<GameObject>("assets/modded/aptrashmarker.prefab");
        TrashLocked = bundle.LoadAsset<Material>("assets/modded/trashlocked.mat");
    }
}
