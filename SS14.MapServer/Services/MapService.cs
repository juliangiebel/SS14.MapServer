using Microsoft.EntityFrameworkCore;
using SS14.MapServer.Models;
using SS14.MapServer.Models.Entities;

namespace SS14.MapServer.Services;

public sealed class MapService
{
    private readonly Context _context;

    public MapService(Context context)
    {
        _context = context;
    }

    public async Task DeleteMap(Map map)
    {
        foreach (var grid in map.Grids)
        {
            var path = grid.Path;

            if (grid.Tiled)
            {
                DeleteDirectory(path);
                var tiles = await _context.Tile!.Where(tile => tile.MapGuid == map.MapGuid && tile.GridId == grid.GridId).ToListAsync();
                _context.Tile!.RemoveRange(tiles);
            }
            else
            {
                DeleteFile(path);
            }


            _context.Remove(grid);
        }

        _context.Map!.Remove(map);
        await _context.SaveChangesAsync();
    }

    private void DeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception _)
        {
            // Best effort deletion
        }
    }

    private void DeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, true);
        }
        catch (Exception _)
        {
            // Best effort deletion
        }
    }
}
