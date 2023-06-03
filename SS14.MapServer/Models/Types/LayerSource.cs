using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace SS14.MapServer.Models.Types;

[Owned]
public class LayerSource
{
    public string Url { get; set; } = "";
    public Area Extent { get; set; } = new();
    public string Composition { get; set; } = "source-over";
    public Point ParallaxScale { get; set; } = new(1, 1);
}
