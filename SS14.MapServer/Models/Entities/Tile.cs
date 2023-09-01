using Microsoft.EntityFrameworkCore;

namespace SS14.MapServer.Models.Entities;

[PrimaryKey(nameof(MapGuid), nameof(GridId), nameof(X), nameof(Y))]
[Index(nameof(MapGuid), nameof(GridId))]
public class Tile
{
    public Guid MapGuid { get; set; }
    public int GridId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Size { get; set; }
    public string Path { get; set; }
    public byte[] Preview { get; set; }

    public Tile(Guid mapGuid, int gridId, int x, int y, int size, string path, byte[] preview)
    {
        MapGuid = mapGuid;
        GridId = gridId;
        X = x;
        Y = y;
        Size = size;
        Path = path;
        Preview = preview;
    }
}
