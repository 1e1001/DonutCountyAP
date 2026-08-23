using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace DonutCountyAP.Randomizer;

[Serializable]
[XmlRoot("randomizer_data")]
public class RandomizerSaveData
{
    [XmlElement("trashopedia_index")]
    public int TrashopediaIndex;
    [XmlElement("uri")]
    public string Uri = "localhost";
    [XmlElement("slot_name")]
    public string SlotName = "Player1";
    [XmlElement("password")]
    public string Password;
    [XmlElement("easier_achievements")]
    public bool EasierAchievements;
    [XmlElement("dialogue_skipping")]
    public bool DialogueSkipping = true;
    // cache un-confirmed locations between game sessions, in case of spurious disconnects or crashes
    [XmlElement("location_cache_id")]
    public string LocationCacheId;
    [XmlElement("location_cache")]
    public HashSet<long> LocationCache;

    public void Validate() { }
    public void ApplyPatches()
    {
        Plugin.Patcher.EasierAchievements.Set(EasierAchievements);
        Plugin.Patcher.DialogueSkipping.Set(DialogueSkipping);
    }
    public void Log()
    {
        Plugin.BepInLogger.LogDebug($"trashopedia_index: {TrashopediaIndex}");
        Plugin.BepInLogger.LogDebug($"connection: {Uri}, {SlotName}");
        Plugin.BepInLogger.LogDebug($"easier_achievements: {EasierAchievements}, dialogue_skipping: {DialogueSkipping}");
    }
}
