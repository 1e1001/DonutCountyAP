using System;
using System.Xml.Serialization;

namespace DonutCountyAP.Patches;

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
    public void Validate() { }
    public void Log()
    {
        Plugin.BepInLogger.LogDebug($"trashopedia_index: {TrashopediaIndex}");
    }
}
