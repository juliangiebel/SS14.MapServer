using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Web;
using BrunoZell.ModelBinding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SS14.MapServer.Configuration;
using SS14.MapServer.Models;
using SS14.MapServer.Models.DTOs;
using SS14.MapServer.Models.Entities;
using SS14.MapServer.Services;
using SS14.MapServer.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SS14.MapServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MapController : ControllerBase
{
    private readonly Context _context;
    private readonly MapService _mapService;
    private readonly FileUploadService _fileUploadService;
    private readonly IJobSchedulingService _schedulingService;

    private readonly ServerConfiguration _serverConfiguration = new();
    private readonly BuildConfiguration _buildConfiguration = new();

    public MapController(
        Context context,
        FileUploadService fileUploadService,
        IJobSchedulingService schedulingService,
        IConfiguration configuration,
        MapService mapService)
    {
        _context = context;
        _fileUploadService = fileUploadService;
        _schedulingService = schedulingService;
        _mapService = mapService;
        configuration.Bind(ServerConfiguration.Name, _serverConfiguration);
    }

    [HttpGet]
    [AllowAnonymous]
    [Produces("application/json")]
    public async Task<ActionResult<IEnumerable<Map>>> GetMaps()
    {
        if (!_buildConfiguration.Enabled)
            return NotFound("Automated building features are disabled");

        if (_context.Map == null)
            return NotFound();

        var maps = await _context.Map.Include(map => map.Grids).ToListAsync();
        maps = maps.Select(SetMapGridUrls).ToList();
        return maps;
    }

    [AllowAnonymous]
    [HttpGet("list/{gitRef}")]
    [Produces("application/json")]
    public async Task<ActionResult<MapList>> GetMapList(string gitRef)
    {
        if (_context.Map == null)
            return NotFound();

        var maps = await _context.Map.Include(map => map.Grids)
            .Where(map => map.GitRef == gitRef)
            .ToListAsync();

        if (maps.Count == 0)
            return new NotFoundResult();

        var mapIds = maps.Select(map => new MapListEntry(map.DisplayName, map.MapId));
        return new MapList(mapIds.ToList());
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    public async Task<ActionResult<Map>> GetMap(Guid id)
    {
        if (_context.Map == null)
            return NotFound();

        var map = await FindMapWithGrids(id);

        if (map == null)
            return NotFound();

        return SetMapGridUrls(map);
    }

    [AllowAnonymous]
    [HttpGet("{id}/{gitRef}")]
    [Produces("application/json")]
    public async Task<ActionResult<Map>> GetMap(string id, string gitRef)
    {
        if (_context.Map == null)
            return NotFound();

        var map = await FindMapWithGrids(id, gitRef);

        if (map == null)
            return NotFound();

        return SetMapGridUrls(map);
    }

    [HttpPut("{id}/{gitRef}")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    public async Task<ActionResult<Map>> PutMap(string id, string gitRef, [FromForm, ModelBinder(BinderType = typeof(JsonModelBinder)), SwaggerRequestBody] Map map, IList<IFormFile> images)
    {
        if (id != map.MapId)
            return BadRequest("The id provided in the path and the map id don't match");

        if (ValidateMapRequest(map, images, out var error))
            return error;

        await _fileUploadService.UploadGridImages(gitRef, map, images);

        if (!MapExists(id, gitRef))
            return await CreateMap(map);

        _context.Entry(map).State = EntityState.Modified;

        foreach (var grid in map.Grids)
        {
            _context.Entry(grid).State = EntityState.Modified;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    public async Task<ActionResult<Map>> PostMap([FromForm, ModelBinder(BinderType = typeof(JsonModelBinder))] Map map, IList<IFormFile> images)
    {
        if (MapExists(map.MapGuid))
            return Conflict();

        if (ValidateMapRequest(map, images, out var error))
            return error;

        var result = await CreateMap(map);

        if (result.Value != null)
            await _fileUploadService.UploadGridImages(map.GitRef, result.Value, images);

        return result;
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMap(Guid id)
    {
        if (_context.Map == null)
        {
            return NotFound();
        }
        var map = await _context.Map
            .Include(map => map.Grids)
            .SingleOrDefaultAsync(map => map.MapGuid == id);

        if (map == null)
        {
            return NotFound();
        }

        await _mapService.DeleteMap(map);
        return NoContent();
    }

    [HttpDelete("{id}/{gitRef}")]
    public async Task<IActionResult> DeleteMap(string id, string gitRef)
    {
        if (_context.Map == null)
        {
            return NotFound();
        }
        var map = await _context.Map
            .Include(map => map.Grids)
            .SingleOrDefaultAsync(map => map.GitRef == gitRef && map.MapId == id.ToLower());

        if (map == null)
        {
            return NotFound();
        }

        await _mapService.DeleteMap(map);
        return NoContent();
    }

    [HttpPost("sync")]
    [Consumes("application/json")]
    public async Task<IActionResult> SyncMaps(List<string>? mapFileNames, bool syncAll, bool forceTiled)
    {
        var data = new JobDataMap
        {
            {Jobs.SyncMaps.MapListKey, mapFileNames ?? new List<string>() },
            {Jobs.SyncMaps.SyncAllKey, syncAll},
            {Jobs.SyncMaps.ForceTiledKey, forceTiled}
        };

        await _schedulingService.RunJob<Jobs.SyncMaps>(nameof(Jobs.SyncMaps), "Sync", data);
        return Ok();
    }

    private bool ValidateMapRequest(Map map, ICollection<IFormFile> images, [NotNullWhen(true)] out BadRequestObjectResult? error)
    {
        //Ensure map id is lowercase
        map.MapId = map.MapId.ToLower();

        if (map.Grids.Count != images.Count)
        {
            error = BadRequest("Amount of uploaded images doesn't match the amount of grids");
            return true;
        }

        var gridIds = map.Grids.Select(grid => grid.GridId).ToList();

        foreach (var image in images)
        {
            //TODO: Check if the extension is allowed

            var name = Path.GetFileNameWithoutExtension(image.FileName);
            if (!int.TryParse(name, out var gridId) || !gridIds.Contains(gridId))
            {
                error = BadRequest($"At least one filename doesn't match any grid id: {HttpUtility.HtmlEncode(name)}");
                return true;
            }
        }

        error = null;
        return false;
    }

    private async Task<Map?> FindMapWithGrids(string id, string gitRef)
    {
        var map = await _context.Map!
            .Include(map => map.Grids)
            .Where(map => map.GitRef == gitRef && map.MapId == id.ToLower())
            .SingleOrDefaultAsync();

        map?.Grids.Sort((grid, grid1) => grid1.Extent.CompareTo(grid.Extent));
        return map;
    }

    private async Task<Map?> FindMapWithGrids(Guid id)
    {
        var map = await _context.Map!
            .Include(map => map.Grids)
            .Where(map => map.MapGuid.Equals(id))
            .SingleOrDefaultAsync();

        map?.Grids.Sort((grid, grid1) => grid1.Extent.CompareTo(grid.Extent));
        return map;
    }

    private async Task<ActionResult<Map>> CreateMap(Map map)
    {
        if (_context.Map == null)
            return Problem("Entity set 'Context.Maps'  is null.");

        _context.Map.Add(map);

        await _context.SaveChangesAsync();
        return CreatedAtAction("GetMap", new { id = map.MapId }, map);
    }

    private bool MapExists(string id, string gitRef)
    {
        return (_context.Map?.Any(e => e.GitRef == gitRef && e.MapId == id.ToLower())).GetValueOrDefault();
    }

    private bool MapExists(Guid id)
    {
        return (_context.Map?.Any(e => e.MapGuid == id)).GetValueOrDefault();
    }

    private Map SetMapGridUrls(Map map)
    {
        foreach (var grid in map.Grids)
        {
            grid.Url = Url.Action(
                "GetGridImage",
                "Image",
                new { id = map.MapGuid, gridId = grid.GridId },
                _serverConfiguration.Host.Scheme,
                $"{_serverConfiguration.Host.Host}:{_serverConfiguration.Host.Port}{_serverConfiguration.PathBase}"
            );

            if (grid.Tiled)
                grid.Url = grid.Url?.Replace("Image/grid", "Tile");
        }
        map.Grids.Sort((grid, grid1) => grid1.Extent.CompareTo(grid.Extent));
        return map;
    }
}
