using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SS14.MapServer.Models;

namespace SS14.MapServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TileController
{
    private readonly Context _context;

    public TileController(Context context)
    {
        _context = context;
    }

    [Produces("application/octet-stream")]
    [HttpGet("{id}/{gridId:int}/{x:int}/{y:int}/{z:int}")]
    public async Task<IActionResult> GetTile(string id, int gridId, int x, int y, int z)
    {
        var map = await _context.Maps.FindAsync(id);
        if (map == null)
            return new NotFoundResult();

        var grid = map.Grids.Find(value => value.GridId.Equals(gridId));
        if (grid == null)
            return new NotFoundResult();

        if (!grid.Tiled)
            return new BadRequestObjectResult(new ApiException($"Grid image with id {gridId} doesn't support image tiling"));

        return new NoContentResult();
    }
}