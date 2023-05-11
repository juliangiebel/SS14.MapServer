namespace SS14.MapServer.Models.Types;

public class ParallaxLayer
{
    public Point Scale {get; set;}
    public Point Offset  {get; set;}
    public bool Static  {get; set;}
    public float MinScale  {get; set;} = 1;
    public LayerSource Source {get; set;}
    public List<LayerSource>? Layers {get; set;}
}