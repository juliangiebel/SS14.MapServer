using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using SS14.MapServer.Models.Types;

namespace SS14.MapServer.Models.DTOs;

public sealed class MapRendererData
{
    public string Id {get; set;}
    [Required, JsonProperty("Name")]
    public string DisplayName {get; set;}
    [JsonProperty("Attributions")]
    public string? Attribution {get; set;}
    [Required]
    public List<GridData> Grids {get;} = new();
    public List<ParallaxLayer> ParallaxLayers {get; set;}
}