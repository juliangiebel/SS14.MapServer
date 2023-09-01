using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using MimeTypes;
using SS14.MapServer.Exceptions;
using SS14.MapServer.Models;
using SS14.MapServer.Models.Entities;

namespace SS14.MapServer.Controllers;

[Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
[ApiController]
public class TileController : ControllerBase
{
    private readonly Context _context;

    public TileController(Context context)
    {
        _context = context;
    }

    [AllowAnonymous]
    [DisableRateLimiting]
    [ResponseCache(CacheProfileName = "Default", VaryByQueryKeys = new[] { "preload" })]
    [HttpGet("{id:guid}/{gridId:int}/{x:int}/{y:int}/{z:int}")]
    [ProducesResponseType(200, Type = typeof(FileStreamResult))]
    [Produces("image/jpg", "image/png", "image/webp", "application/json")]
    public async Task<IActionResult> GetTile(Guid id, int gridId, int x, int y, int z, [FromQuery] bool preload)
    {
        // TODO cache the result of this method
        var (map, grid) = await RetrieveMapAndGrid(id, gridId);

        if (map == null || grid == null)
            return new NotFoundResult();

        var hash = $@"""{map.MapGuid:N}{Convert.ToInt32(preload) + x + y + gridId + map.LastUpdated.GetHashCode():X}""";
        if (CheckETags(hash, out var result))
            return result;


        if (!grid.Tiled)
            return new BadRequestObjectResult(new ApiErrorMessage($"Grid image with id {gridId} doesn't support image tiling"));

        var tile = await _context.Tile!.FindAsync(id, gridId, x, y);

        if (tile == null || !System.IO.File.Exists(tile.Path))
            return new OkResult();

        if (preload)
            return File(tile.Preview, "image/webp", true);

        var file = new FileStream(tile.Path, FileMode.Open);
        var mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(tile.Path));

        return File(file, mimeType, map.LastUpdated, new EntityTagHeaderValue(hash), true);
    }

    private async Task<(Map? map, Grid? grid)> RetrieveMapAndGrid(Guid id, int gridId)
    {
        var map = await _context.Map!
            .Include(map => map.Grids)
            .Where(map => map.MapGuid.Equals(id))
            .SingleOrDefaultAsync();

        if (map == null)
            return (null, null);

        var grid = map.Grids.Find(value => value.GridId.Equals(gridId));
        return (map, grid);
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
