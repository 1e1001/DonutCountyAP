using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using UnityEngine.SocialPlatforms;
using UnityEngine;

namespace DonutCountyAP.Randomizer;

public class GameOptions
{
    public enum GoalAreaMode
    {
        Bossfight,
        Aftermath,
    }
    public enum CatapultMode
    {
        Off,
        Global,
        Individual,
    }
    // Game options
    [JsonProperty("goal_area"), JsonConverter(typeof(StringEnumConverter))]
    public GoalAreaMode GoalArea;
    // differs from options struct! these two are adjusted to the exact values for this generation
    [JsonProperty("total_fragments")]
    public int TotalFragments;
    [JsonProperty("required_fragments")]
    public int RequiredFragments;

    // Location options
    [JsonProperty("water")]
    public bool Water;
    [JsonProperty("fire")]
    public bool Fire;
    [JsonProperty("snake")]
    public bool Snake;
    [JsonProperty("light")]
    public bool Light;
    [JsonProperty("bunnies")]
    public bool Bunnies;
    [JsonProperty("catapult"), JsonConverter(typeof(StringEnumConverter))]
    public CatapultMode Catapult;

    // Item options
    [JsonProperty("level_completions")]
    public bool LevelCompletions;
    [JsonProperty("level_segments")]
    public bool LevelSegments;
    [JsonProperty("achievements")]
    public bool Achievements;
    [JsonProperty("buy_catapult")]
    public bool BuyCatapult;
    [JsonProperty("snake_danger")]
    public bool SnakeDanger;
    [JsonProperty("salt_and_pepper")]
    public bool SaltAndPepper;
    [JsonProperty("hack_protocol")]
    public bool HackProtocol;

}
