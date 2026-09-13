using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DonutCountyAP.Generated;
using System;

namespace DonutCountyAP.Randomizer;

public class Logic
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
        Trash,
        TrashType,
    }

    class JsonArray2Converter : JsonConverter
    {
        public override bool CanConvert(Type _objectType) => true;
        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var constructor = objectType.GetConstructors()[0];
            var constructorParameters = constructor.GetParameters();
            var parameters = new object[constructorParameters.Length];
            var parameterIndex = 0;
            for (var i = 0; i < constructorParameters.Length; ++i)
                parameters[i] = constructorParameters[i].DefaultValue;
            reader.Read(); // StartArray
            while (reader.TokenType != JsonToken.EndArray)
            {
                var value = serializer.Deserialize(reader, constructorParameters[parameterIndex].ParameterType);
                parameters[parameterIndex++] = value;
                reader.Read();
            }
            return constructor.Invoke([.. parameters]);
        }

        public override void WriteJson(JsonWriter _writer, object _value, JsonSerializer _serializer) => throw new NotImplementedException();
    }

    [JsonConverter(typeof(JsonArray2Converter))]
    public record struct GameLocation(int Id, LocationType Type);
    [JsonConverter(typeof(JsonArray2Converter))]
    public record struct GameTrash(string Name, ItemId Unlock = ItemId.None, int TypeId = -1, int Id = -1);
    public record struct GameSegment(GameLocation[] Locations, int Trash, int Types);
    public record struct GameLevel(ItemId Unlock, GameSegment[] Segments);
    [JsonConverter(typeof(JsonArray2Converter))]
    public record struct DebugLocation(int Id, LocationType Type, string Name);

    public const int GOAL = 0;
    public Dictionary<string, GameLocation> Events;
    public int LocationsSize;
    public Dictionary<string, GameTrash[]> Trash;
    public GameLevel[] Levels;
    public Dictionary<int, int[]> TrashEvents;
    public int[] Counters;
    public ItemId[] DebugSortedItems;
    public DebugLocation[] DebugSortedLocations;

    public static Logic LoadEmbedded()
    {
        var asm = Assembly.GetExecutingAssembly();
        var fullPath = $"{asm.GetName().Name}.Generated.logic.json";
        using var stream = asm.GetManifestResourceStream(fullPath);
        using var reader = new StreamReader(stream);
        using var jsonReader = new JsonTextReader(reader);
        var json = new JsonSerializer();
        return json.Deserialize<Logic>(jsonReader);
    }
}
