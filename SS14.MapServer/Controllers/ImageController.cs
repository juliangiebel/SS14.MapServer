using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MimeTypes;
using Serilog;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using SS14.MapServer.Configuration;
using SS14.MapServer.Exceptions;
using SS14.MapServer.Models;
using SS14.MapServer.Models.Entities;
using SS14.MapServer.Services;

namespace SS14.MapServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ImageController : ControllerBase
{
    private const int MinWidth = 32;

    private readonly CacheConfiguration _cacheConfiguration = new();

    private readonly Context _context;
    private readonly FileUploadService _fileUploadService;
    private readonly IDistributedCache _cache;

    public ImageController(Context context, FileUploadService fileUploadService, IDistributedCache cache, IConfiguration configuration)
    {
        _context = context;
        _fileUploadService = fileUploadService;
        _cache = cache;

        configuration.Bind(CacheConfiguration.Name, _cacheConfiguration);
    }

    [ResponseCache(CacheProfileName = "Default")]
    [AllowAnonymous]
    [Produces("image/jpg", "image/png", "image/webp", "application/json")]
    [ProducesResponseType(200, Type = typeof(FileStreamResult))]
    [HttpGet("grid/{id:guid}/{gridId:int}")]
    public async Task<IActionResult> GetGridImage(Guid id, int gridId, [FromQuery] int? width)
    {
        if (width is < MinWidth)
            return new BadRequestObjectResult($"Width can't be smaller than {MinWidth} pixel");

        var map = await _context.Maps!
            .Include(map => map.Grids)
            .Where(map => map.MapGuid.Equals(id))
            .SingleOrDefaultAsync();

        return await InternalGetMap(map, gridId, width);
    }

    [ResponseCache(CacheProfileName = "Default")]
    [AllowAnonymous]
    [Produces("image/jpg", "image/png", "image/webp", "application/json")]
    [ProducesResponseType(200, Type = typeof(FileStreamResult))]
    [HttpGet("grid/{id}/{gitRef}/{gridId:int}")]
    public async Task<IActionResult> GetGridImage(string id, string gitRef, int gridId, [FromQuery] int? width)
    {
        if (width is < MinWidth)
            return new BadRequestObjectResult($"Width can't be smaller than {MinWidth} pixel");

        var map = await _context.Maps!
            .Include(map => map.Grids)
            .Where(map => map.GitRef.Equals(gitRef) && map.MapId.Equals(id))
            .SingleOrDefaultAsync();

        return await InternalGetMap(map, gridId, width);
    }

    [ResponseCache(CacheProfileName = "Default")]
    [AllowAnonymous]
    [Produces("image/jpg", "image/png", "image/webp")]
    [ProducesResponseType(200, Type = typeof(FileStreamResult))]
    [HttpGet("file/{*path}")]
    public async Task<IActionResult> GetImage(string path, [FromQuery] int? width)
    {
        if (width is < MinWidth)
            return new BadRequestObjectResult($"Width can't be smaller than {MinWidth} pixel");

        var image = await _context.Images!.FindAsync(path);

        if (image == null)
            return new NotFoundResult();

        var key = $"{path}{width}";
        var data = await _cache.GetAsync(key);
        var mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(image.InternalPath));

        if (data != null)
            return File(data, mimeType, Path.GetFileName(image.InternalPath));


        if (!System.IO.File.Exists(image.InternalPath))
        {
            Log.Error("File doesn't exist for image saved in database with internal path: {Path}", path);
            return new NotFoundResult();
        }

        var file = new FileStream(image.InternalPath, FileMode.Open);

        if (!width.HasValue)
            return File(file, mimeType, Path.GetFileName(path));

        data = await ResizeAndCacheAsync(key, file, width.Value, path);
        await file.DisposeAsync();

        return File(data, mimeType, Path.GetFileName(image.InternalPath));
    }

    [Consumes("multipart/form-data")]
    [HttpPost("upload/{*path}")]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public async Task<IActionResult> UploadImage(string path, IFormFile file)
    {
        if (_fileUploadService.ValidateImageFile(file, out var message))
            return new BadRequestObjectResult(new ApiErrorMessage(message));

        var image = await _context.Images!.FindAsync(path);

        if (image == null)
        {
            image = new ImageFile();
            _context.Add(image);
        }

        image.Path = path;

        await _fileUploadService.UploadImage(image, file, image.InternalPath);

        await _context.SaveChangesAsync();

        return new OkResult();
    }

    private async Task<IActionResult> InternalGetMap(Map? map, int gridId, int? width = null)
    {
        if (map == null)
            return new NotFoundResult();

        var grid = map.Grids.Find(value => value.GridId.Equals(gridId));
        if (grid == null)
            return new NotFoundResult();

        if (grid.Tiled)
            return new BadRequestObjectResult(new ApiErrorMessage($"Grid image with id {gridId} is a tiled image"));

        var key = $"{map.MapGuid}{gridId}{width}";
        var data = await _cache.GetAsync(key);
        var mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(grid.Path));

        if (data != null)
            return File(data, mimeType, Path.GetFileName(grid.Path));

        if (!System.IO.File.Exists(grid.Path))
            return new NotFoundResult();

        var file = new FileStream(grid.Path, FileMode.Open);

        if (!width.HasValue)
            return File(file, mimeType, Path.GetFileName(grid.Path));

        data = await ResizeAndCacheAsync(key, file, width.Value, grid.Path);
        await file.DisposeAsync();

        return File(data, mimeType, Path.GetFileName(grid.Path));
    }

    private async Task<byte[]> ResizeAndCacheAsync(string key, Stream file, int width, string path)
    {
        using var output = new MemoryStream();
        using var image = await Image.LoadAsync(file);

        image.Mutate(context => context.Resize(new Size(width, 0), new NearestNeighborResampler(), false));
        await image.SaveAsync(output, image.DetectEncoder(path));

        var data = output.ToArray();
        await _cache.SetAsync(key, data, new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(_cacheConfiguration.SlidingExpirationMinutes),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheConfiguration.RelativeAbsoluteExpirationMinutes)
        });

        return data;
    }
}
