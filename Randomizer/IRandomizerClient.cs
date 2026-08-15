using System.Collections.Generic;

namespace DonutCountyAP.Randomizer;

public interface IRandomizerClient
{
    public void Update();
    public string GUIStatus();
    public bool IsComplete();
    public void SendGoal();
    public void SendLocation(long id);
    public void Disconnect();
    public ICollection<long> Locations();
    public void SetSlotData(string key, int value);
    public void SetSlotDataMax(string key, int value);
}
