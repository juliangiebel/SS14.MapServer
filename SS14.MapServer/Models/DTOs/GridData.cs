using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using SS14.MapServer.Models.Types;
using Point = SS14.MapServer.Models.Types.Point;

namespace SS14.MapServer.Models;

public sealed class GridData
{
    [Required]
    public int GridId {get; set;}
    public bool Tiled {get; set;} //Not yet supported
    public Point Offset {get; set;} = new(0, 0);
    [Required]
    public Area Extent { get; set; } = default!;

    [Required, JsonProperty("Url")]
    public string Path { get; set; } = default!;
}