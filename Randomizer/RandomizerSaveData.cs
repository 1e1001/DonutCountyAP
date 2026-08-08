using System;
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
    public bool DialogueSkipping;


    public void Validate() { }
    public void ApplyPatches()
    {
        Plugin.Patcher.EasierAchievements.Set(EasierAchievements);
        Plugin.Patcher.DialogueSkipping.Set(DialogueSkipping);
    }
    public void Log()
    {
        Plugin.BepInLogger.LogDebug($"trashopedia_index: {TrashopediaIndex}");
    }
}
