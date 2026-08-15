using System.Collections.Generic;

namespace DonutCountyAP.Randomizer;

public class DebugClient : IRandomizerClient
{
    readonly HashSet<long> _locations = [];
    public Dictionary<long, ItemId> FakeRandomizer = [];
    bool _complete = false;

    // impl IRandomizerClient
    public void Update() { }
    public string GUIStatus()
    {
        return "In debug session";
    }
    public bool IsComplete()
    {
        return _complete;
    }
    public void SendGoal()
    {
        _complete = true;
    }
    public void SendLocation(long id)
    {
        if (_locations.Add(id) && FakeRandomizer.TryGetValue(id, out var item))
            Plugin.GameState.ReceivedItem(item);
    }
    public void Disconnect()
    {
        Plugin.Client = null;
        Plugin.SetGame(null);
    }
    public ICollection<long> Locations()
    {
        return _locations;
    }
    public void SetSlotData(string _key, int _value) { }
    public void SetSlotDataMax(string _key, int _value) { }
}
