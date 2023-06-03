using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SS14.MapServer.Models.Types;
using Point = SS14.MapServer.Models.Types.Point;

namespace SS14.MapServer.Models.Entities;

public class Grid
{
    [Key]
    public Guid Id {get; set;}
    [Required]
    public int GridId {get; set;}
    public bool Tiled {get; set;}
    public int TileSize { get; set; } = 256;
    public Point Offset {get; set;} = new(0, 0);
    [Required]
    public Area Extent { get; set; } = default!;

    [Required, JsonIgnore]
    public string Path { get; set; } = default!;

    [NotMapped]
    public string? Url { get; set; }
}
