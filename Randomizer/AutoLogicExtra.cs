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
        Victory,
        SnakeDanger,
        Catapult,
        SaltAndPepper,
        HackProtocol,
    }
    public record struct Location(long Id, LocationType Type);
    public record struct DebugTracker(long Id, string Name)
}
