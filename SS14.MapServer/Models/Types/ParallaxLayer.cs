using Microsoft.EntityFrameworkCore;

namespace SS14.MapServer.Models.Types;

[Owned]
public class ParallaxLayer
{
    public Point Scale { get; set; } = new();
    public Point Offset { get; set; } = new();
    public bool Static  {get; set;}
    public float? MinScale {get; set;} = 1;
    public LayerSource Source { get; set; } = new();
    public List<LayerSource>? Layers {get; set;}
}