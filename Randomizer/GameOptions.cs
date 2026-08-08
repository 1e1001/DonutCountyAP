using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using UnityEngine.SocialPlatforms;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

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
        Split,
    }
    // Game options
    [JsonProperty("goal_area"), JsonConverter(typeof(StringEnumConverter))]
    public GoalAreaMode GoalArea;
    // differs from options struct! these two are adjusted to the exact values for this generation
    [JsonProperty("total_fragments")]
    public int TotalFragments;
    [JsonProperty("required_fragments")]
    public int[] RequiredFragments;

    // Item options
    [JsonProperty("levels")]
    public bool Levels;
    [JsonProperty("hole_water")]
    public bool HoleWater;
    [JsonProperty("hole_fire")]
    public bool HoleFire;
    [JsonProperty("hole_snake")]
    public bool HoleSnake;
    [JsonProperty("hole_light")]
    public bool HoleLight;
    [JsonProperty("hole_bunnies")]
    public bool HoleBunnies;
    [JsonProperty("catapult"), JsonConverter(typeof(StringEnumConverter))]
    public CatapultMode Catapult;

    // Location options
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


    public void ApplyPatches()
    {
        Plugin.Patcher.SnakeDanger.Set(SnakeDanger);
        Plugin.Patcher.SaltAndPepper.Set(SaltAndPepper);
        Plugin.Patcher.HackProtocol.Set(HackProtocol);
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
            case AutoLogic.LocationType.HackProtocol:
                return HackProtocol;
            case AutoLogic.LocationType.Victory:
                return true;
            default:
                Plugin.BepInLogger.LogError($"tried to send location with nonexistant type {type}");
                return false;
        }
    }
}
