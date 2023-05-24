using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeTypes;
using SS14.MapServer.Models;

namespace SS14.MapServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TileController : ControllerBase
{
    private readonly Context _context;

    public TileController(Context context)
    {
        _context = context;
    }
    
    [AllowAnonymous]
    [Produces("image/jpg", "image/png", "image/webp")]
    [ProducesResponseType(200, Type = typeof(FileStreamResult))]
    [HttpGet("{id}/{gridId:int}/{x:int}/{y:int}/{z:int}")]
    public async Task<IActionResult> GetTile(string id, int gridId, int x, int y, int z)
    {
        var map = await _context.Maps!
            .Include(map => map.Grids)
            .Where(map => map.Id.Equals(id))
            .SingleOrDefaultAsync();
        
        if (map == null)
            return new NotFoundResult();

        var grid = map.Grids.Find(value => value.GridId.Equals(gridId));
        if (grid == null)
            return new NotFoundResult();

        if (!grid.Tiled)
            return new BadRequestObjectResult(new ApiException($"Grid image with id {gridId} doesn't support image tiling"));

        var tile = await _context.Tiles!.FindAsync(id, gridId, x, y);

        if (tile == null)
            return new NotFoundResult();
        
        var file = new FileStream(tile.Path, FileMode.Open);
        var mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(tile.Path));
        
        return File(file, mimeType, Path.GetFileName(tile.Path));
    }
}