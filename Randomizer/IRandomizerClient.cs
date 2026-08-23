using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace DonutCountyAP.Randomizer;

public interface IRandomizerClient
{
    public bool Connecting();
    public string GUIStatus();
    public void SendChat(string text);
    public void SendGoal();
    public void SendLocation(long id);
    public void Disconnect();
    public void SetSlotStorage(string key, JToken value);
}
