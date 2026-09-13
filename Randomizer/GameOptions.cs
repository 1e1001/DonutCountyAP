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
    public enum TrashsanityMode
    {
        Off,
        Types,
        All,
    }
    [JsonProperty("version")]
    public string Version = "0.0.0";

    // Game options
    [JsonProperty("goal_area"), JsonConverter(typeof(StringEnumConverter))]
    public GoalAreaMode GoalArea;
    // differs from options struct! these two are adjusted to the exact values for this generation
    [JsonProperty("total_pieces")]
    public int TotalPieces;

    // Item options
    [JsonProperty("levels")]
    public bool Levels;
    [JsonProperty("hole"), JsonConverter(typeof(StringEnumConverter))]
    public EffectItemMode Hole;
    [JsonProperty("catapult"), JsonConverter(typeof(StringEnumConverter))]
    public EffectItemMode Catapult;
    [JsonProperty("texting")]
    public bool Texting;
    [JsonProperty("trash_souls")]
    public bool TrashSouls;

    // Location options
    [JsonProperty("level_completions")]
    public bool LevelCompletions = true;
    [JsonProperty("level_segments")]
    public bool LevelSegments = true;
    [JsonProperty("achievements")]
    public bool Achievements;
    [JsonProperty("snake_danger")]
    public bool SnakeDanger;
    [JsonProperty("salt_and_pepper")]
    public bool SaltAndPepper;
    [JsonProperty("trashsanity"), JsonConverter(typeof(StringEnumConverter))]
    public TrashsanityMode Trashsanity;

    // also Game Options but moved to the end for easier debug
    [JsonProperty("required_pieces")]
    public int[] RequiredPieces = new int[22];


    public void ApplyPatches()
    {
        Plugin.Patcher.SnakeDanger.Set(SnakeDanger);
        Plugin.Patcher.SaltAndPepper.Set(SaltAndPepper);
        // TrashSouls is only enabled if trashsanity is on
        Plugin.Patcher.Trashsanity.Set(Trashsanity != TrashsanityMode.Off);
    }
    public static void UnpatchAll()
    {
        Plugin.Patcher.SnakeDanger.Set(false);
        Plugin.Patcher.SaltAndPepper.Set(false);
        Plugin.Patcher.Trashsanity.Set(false);
    }

    public bool CanSendLocation(Logic.LocationType type)
    {
        switch (type)
        {
            case Logic.LocationType.Delivery:
                return LevelCompletions;
            case Logic.LocationType.Segment:
                return LevelSegments;
            case Logic.LocationType.Achievement:
                return Achievements;
            case Logic.LocationType.SnakeDanger:
                return SnakeDanger;
            case Logic.LocationType.Catapult:
                return Catapult != EffectItemMode.Off;
            case Logic.LocationType.SaltAndPepper:
                return SaltAndPepper;
            case Logic.LocationType.Victory:
                return true;
            case Logic.LocationType.Trash:
                return Trashsanity == TrashsanityMode.All;
            case Logic.LocationType.TrashType:
                return Trashsanity == TrashsanityMode.Types;
            default:
                Plugin.BepInLogger.LogError($"tried to send location with mysterious type {type}");
                return false;
        }
    }
}
