using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using MimeTypes;
using SS14.MapServer.Exceptions;
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
    [ResponseCache(CacheProfileName = "Default")]
    [HttpGet("{id:guid}/{gridId:int}/{x:int}/{y:int}/{z:int}")]
    [ProducesResponseType(200, Type = typeof(FileStreamResult))]
    [Produces("image/jpg", "image/png", "image/webp", "application/json")]
    public async Task<IActionResult> GetTile(Guid id, int gridId, int x, int y, int z)
    {
        var map = await _context.Map!
            .Include(map => map.Grids)
            .Where(map => map.MapGuid.Equals(id))
            .SingleOrDefaultAsync();

        if (map == null)
            return new NotFoundResult();

        var hash = $@"""{map.MapGuid:N}{map.LastUpdated.GetHashCode():X}""";
        if (CheckETags(hash, out var result))
            return result;

        var grid = map.Grids.Find(value => value.GridId.Equals(gridId));
        if (grid == null)
            return new NotFoundResult();

        if (!grid.Tiled)
            return new BadRequestObjectResult(new ApiErrorMessage($"Grid image with id {gridId} doesn't support image tiling"));

        var tile = await _context.Tile!.FindAsync(id, gridId, x, y);

        if (tile == null || !System.IO.File.Exists(grid.Path))
            return new NotFoundResult();

        var file = new FileStream(tile.Path, FileMode.Open);
        var mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(tile.Path));

        return File(file, mimeType, map.LastUpdated, new EntityTagHeaderValue(hash), true);
    }

    private bool CheckETags(string hash, [NotNullWhen(true)] out IActionResult? result)
    {
        if (Request.Headers.IfNoneMatch.Any(h => h != null && h.Equals(hash)))
        {
            result = new StatusCodeResult(StatusCodes.Status304NotModified);
            return true;
        }

        result = null;
        return false;
    }
}
