using Microsoft.EntityFrameworkCore;

namespace SS14.MapServer.Models.Entities;
#pragma warning disable CS8618

[PrimaryKey(nameof(MapId), nameof(GridId), nameof(X), nameof(Y))]
[Index(nameof(MapId), nameof(GridId))]
public class Tile
{
    public string MapId { get; set; }
    public int GridId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Size { get; set; }
    public string Path { get; set; }

    public Tile(string mapId, int gridId, int x, int y, int size, string path)
    {
        MapId = mapId;
        GridId = gridId;
        X = x;
        Y = y;
        Size = size;
        Path = path;
    }
}