using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using MimeTypes;
using Serilog;
using SS14.MapServer.Exceptions;
using SS14.MapServer.Models;
using SS14.MapServer.Models.Entities;
using SS14.MapServer.Services;

namespace SS14.MapServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ImageController : ControllerBase
{
    private readonly Context _context;
    private readonly FileUploadService _fileUploadService;

    public ImageController(Context context, FileUploadService fileUploadService)
    {
        _context = context;
        _fileUploadService = fileUploadService;
    }

    [AllowAnonymous]
    [ResponseCache(CacheProfileName = "Default")]
    [HttpGet("grid/{id:guid}/{gridId:int}")]
    [Produces("image/jpg", "image/png", "image/webp", "application/json")]
    [ProducesResponseType(200, Type = typeof(FileStreamResult))]
    public async Task<IActionResult> GetGridImage(Guid id, int gridId)
    {
        var map = await _context.Map!
            .Include(map => map.Grids)
            .Where(map => map.MapGuid.Equals(id))
            .SingleOrDefaultAsync();

        return await InternalGetMap(map, gridId);
    }

    [AllowAnonymous]
    [ResponseCache(CacheProfileName = "Default")]
    [HttpGet("grid/{id}/{gitRef}/{gridId:int}")]
    [ProducesResponseType(200, Type = typeof(FileStreamResult))]
    [Produces("image/jpg", "image/png", "image/webp", "application/json")]
    public async Task<IActionResult> GetGridImage(string id, string gitRef, int gridId)
    {
        var map = await _context.Map!
            .Include(map => map.Grids)
            .Where(map => map.GitRef.Equals(gitRef) && map.MapId.Equals(id))
            .SingleOrDefaultAsync();

        return await InternalGetMap(map, gridId);
    }

    [HttpPost("upload/{*path}")]
    [Consumes("multipart/form-data")]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public async Task<IActionResult> UploadImage(string path, IFormFile file)
    {
        if (_fileUploadService.ValidateImageFile(file, out var message))
            return new BadRequestObjectResult(new ApiErrorMessage(message));

        var image = await _context.Image!.FindAsync(path);

        if (image == null)
        {
            image = new ImageFile();
            image.Path = path;
            _context.Add(image);
        }
        // TODO: Delete previous image file if image already exists in database
        image.Path = path;

        await _fileUploadService.UploadImage(image, file, image.InternalPath);

        await _context.SaveChangesAsync();

        return new OkResult();
    }

    [AllowAnonymous]
    [HttpGet("file/{*path}")]
    [ResponseCache(CacheProfileName = "Default")]
    [Produces("image/jpg", "image/png", "image/webp")]
    [ProducesResponseType(200, Type = typeof(FileStreamResult))]
    public async Task<IActionResult> GetImage(string path)
    {
        var image = await _context.Image!.FindAsync(path);

        if (image == null)
            return new NotFoundResult();

        var hash = $@"""{image.Path.GetHashCode():X}{image.LastUpdated.GetHashCode():X}""";
        if (CheckETags(hash, out var response))
            return response;

        if (!System.IO.File.Exists(image.InternalPath))
        {
            Log.Error("File doesn't exist for image saved in database with internal path: {Path}", path);
            return new NotFoundResult();
        }

        var mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(image.InternalPath));
        var file = new FileStream(image.InternalPath, FileMode.Open);

        try
        {
            return File(file, mimeType, image.LastUpdated, new EntityTagHeaderValue(hash), true);
        }
        catch
        {
            await file.DisposeAsync();
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
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

    private async Task<IActionResult> InternalGetMap(Map? map, int gridId)
    {
        if (map == null)
            return new NotFoundResult();

        var hash = $@"""{map.MapGuid:N}{map.LastUpdated.GetHashCode():X}""";
        if (CheckETags(hash, out var result))
            return result;

        var grid = map.Grids.Find(value => value.GridId.Equals(gridId));
        if (grid == null)
            return new NotFoundResult();

        if (grid.Tiled)
            return new BadRequestObjectResult(new ApiErrorMessage($"Grid image with id {gridId} is a tiled image"));

        if (!System.IO.File.Exists(grid.Path))
            return new NotFoundResult();

        var file = new FileStream(grid.Path, FileMode.Open);
        var mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(grid.Path));

        try
        {
            return File(file, mimeType, map.LastUpdated, new EntityTagHeaderValue(hash), true);
        }
        catch
        {
            await file.DisposeAsync();
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
