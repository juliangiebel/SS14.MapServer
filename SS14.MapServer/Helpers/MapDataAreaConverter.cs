using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SS14.MapServer.Models.Types;
using Point = SS14.MapServer.Models.Types.Point;

namespace SS14.MapServer.Helpers;

public class MapDataAreaConverter : JsonConverter<Area>
{
    public override bool CanWrite => false;

    //This will never get called
    public override void WriteJson(JsonWriter writer, Area? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
    
    public override Area? ReadJson(
        JsonReader reader, 
        Type objectType, 
        Area? existingValue, 
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var token = JToken.ReadFrom(reader);
        var a = new Point
        {
            X = token.Value<float>("X1"),
            Y = token.Value<float>("Y1")
        };
        
        var b = new Point
        {
            X = token.Value<float>("X2"),
            Y = token.Value<float>("Y2")
        };

        return new Area
        {
            A = a,
            B = b
        };
    }
}

public sealed record PropertyPath(string PropertyName, string JsonPath);