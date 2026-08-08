using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DonutCountyAP.Randomizer;

public partial class AutoLogic
{
    public enum LocationType
    {
        Delivery,
        Segment,
        Achievement,
        SnakeDanger,
        Catapult,
        SaltAndPepper,
        HackProtocol,
        Victory,
    }
    public record struct Location(long Id, LocationType Type);
    // TODO: the real tracker info will be shaped for the menus
    public record struct DebugTracker(string Name, Location Location);

    public record struct LevelSelect(ItemId Unlock, Location[] Locations);

}
