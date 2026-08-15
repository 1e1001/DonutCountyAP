using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DonutCountyAP.Randomizer;

public class GameOptions
{
    public enum GoalAreaMode
    {
        Bossfight,
        Aftermath,
    }
    public enum EffectItemMode
    {
        Off,
        Global,
        Split,
    }
    // Game options
    [JsonProperty("goal_area"), JsonConverter(typeof(StringEnumConverter))]
    public GoalAreaMode GoalArea;
    // differs from options struct! these two are adjusted to the exact values for this generation
    [JsonProperty("total_pieces")]
    public int TotalPieces;
    [JsonProperty("required_pieces")]
    public int[] RequiredPieces = new int[22];

    // Item options
    [JsonProperty("levels")]
    public bool Levels;
    [JsonProperty("hole"), JsonConverter(typeof(StringEnumConverter))]
    public EffectItemMode Hole;
    [JsonProperty("catapult"), JsonConverter(typeof(StringEnumConverter))]
    public EffectItemMode Catapult;
    [JsonProperty("texting")]
    public bool Texting;

    // Location options
    [JsonProperty("level_completions")]
    public bool LevelCompletions = true;
    [JsonProperty("level_segments")]
    public bool LevelSegments = true;
    [JsonProperty("achievements")]
    public bool Achievements;
    [JsonProperty("buy_catapult")]
    public bool BuyCatapult;
    [JsonProperty("snake_danger")]
    public bool SnakeDanger;
    [JsonProperty("salt_and_pepper")]
    public bool SaltAndPepper;


    public void ApplyPatches()
    {
        Plugin.Patcher.SnakeDanger.Set(SnakeDanger);
        Plugin.Patcher.SaltAndPepper.Set(SaltAndPepper);
    }

    public bool CanSendLocation(AutoLogic.LocationType type)
    {
        switch (type)
        {
            case AutoLogic.LocationType.Delivery:
                return LevelCompletions;
            case AutoLogic.LocationType.Segment:
                return LevelSegments;
            case AutoLogic.LocationType.Achievement:
                return Achievements;
            case AutoLogic.LocationType.SnakeDanger:
                return SnakeDanger;
            case AutoLogic.LocationType.Catapult:
                return BuyCatapult;
            case AutoLogic.LocationType.SaltAndPepper:
                return SaltAndPepper;
            case AutoLogic.LocationType.Victory:
                return true;
            default:
                Plugin.BepInLogger.LogError($"tried to send location with mysterious type {type}");
                return false;
        }
    }
}
