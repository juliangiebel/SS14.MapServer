using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SS14.MapServer.Models.Types;

namespace SS14.MapServer.Models.Entities;

[Index(nameof(GitRef), nameof(MapId))]
public class Map
{
    /// <summary>
    /// The internal map id. Named MapGuid to prevent ambiguity wit MapId if it where just called Id
    /// </summary>
    [Key]
    public Guid MapGuid { get; set; }
    [Required]
    public string GitRef { get; set; } = default!;
    [Required]
    public string MapId { get; set; } = default!;
    [Required]
    public string DisplayName { get; set; } = default!;
    public string? Attribution {get; set;}
    [Required]
    public List<Grid> Grids {get;} = new();

    [Column(TypeName = "jsonb")]
    public List<ParallaxLayer> ParallaxLayers { get; set; } = new();

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}
