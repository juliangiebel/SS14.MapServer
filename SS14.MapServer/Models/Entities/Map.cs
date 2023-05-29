using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SS14.MapServer.Models.Types;

namespace SS14.MapServer.Models.Entities;

public class Map
{
    public string Id { get; set; } = default!;
    [Required] 
    public string DisplayName { get; set; } = default!;
    public string? Attribution {get; set;}
    [Required]
    public List<Grid> Grids {get;} = new();

    [Column(TypeName = "jsonb")]
    public List<ParallaxLayer> ParallaxLayers { get; set; } = new();
}