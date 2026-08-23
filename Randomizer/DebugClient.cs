using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace DonutCountyAP.Randomizer;

public class DebugClient : IRandomizerClient
{
    public Dictionary<long, ItemId> FakeRandomizer = [];

    // impl IRandomizerClient
    public bool Connecting() => false;
    public string GUIStatus() => "Debug session";
    public void SendChat(string _text) { }
    public void SendGoal() { }
    public void SendLocation(long id)
    {
        if (FakeRandomizer.TryGetValue(id, out var item))
            Plugin.GameState.ReceivedItem(item);
    }
    public void Disconnect() => Plugin.SetGame(null);
    public void SetSlotStorage(string _key, JToken _value) { }
}
