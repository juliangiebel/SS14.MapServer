using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SS14.MapServer.Models.Types;

namespace SS14.MapServer.Models.Entities;
#pragma warning disable CS8618

public class Map
{
    public string Id {get; set;}
    [Required]
    public string DisplayName {get; set;}
    public string? Attribution {get; set;}
    [Required]
    public List<Grid> Grids {get;} = new List<Grid>();
    
    [Column(TypeName = "jsonb")]
    public List<ParallaxLayer> ParallaxLayers {get; set;}
}