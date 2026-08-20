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
        Victory,
    }
    public const int LOCATION_GOAL = 0;
    // Id is an int instead of a long, since i use densely-packed location ids
    public record struct Location(int Id, LocationType Type);
    // TODO: the real tracker info will be shaped for the menus
    public record struct DebugTracker(string Name, Location Location);

    public record struct LevelSelect(ItemId Unlock, Location[] Locations);

}
