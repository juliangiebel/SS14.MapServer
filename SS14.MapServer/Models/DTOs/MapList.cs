using Newtonsoft.Json;

namespace SS14.MapServer.Models.DTOs;

public sealed class MapList
{
    [JsonProperty("maps")]
    public List<MapListEntry> Maps { get; set; }

    public MapList(List<MapListEntry> maps)
    {
        Maps = maps;
    }
}

public sealed class MapListEntry
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }


    public MapListEntry(string name, string id)
    {
        Name = name;
        Id = id;
    }
}
