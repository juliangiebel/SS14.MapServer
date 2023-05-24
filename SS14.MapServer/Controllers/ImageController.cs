﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeTypes;
using Serilog;
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
    [Produces("image/jpg", "image/png", "image/webp", "application/json")]
    [ProducesResponseType(200, Type = typeof(FileStreamResult))]
    [HttpGet("grid/{id}/{gridId:int}")]
    public async Task<IActionResult> GetImage(string id, int gridId)
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
        
        if (grid.Tiled)
            return new BadRequestObjectResult(new ApiErrorMessage($"Grid image with id {gridId} is a tiled image"));
        
        if (!System.IO.File.Exists(grid.Path))
            return new NotFoundResult();
        
        var file = new FileStream(grid.Path, FileMode.Open);
        var mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(grid.Path));
        
        return File(file, mimeType, Path.GetFileName(grid.Path));
    }

    [AllowAnonymous]
    [Produces("image/jpg", "image/png", "image/webp")]
    [ProducesResponseType(200, Type = typeof(FileStreamResult))]
    [HttpGet("file/{*path}")]
    public async Task<IActionResult> GetMapImage(string path)
    {
        var image = await _context.Images!.FindAsync(path);

        if (image == null)
            return new NotFoundResult();

        if (!System.IO.File.Exists(image.InternalPath))
        {
            Log.Error("File doesn't exist for image saved in database with internal path: {Path}", path);
            return new NotFoundResult();
        }
        
        var file = new FileStream(image.InternalPath, FileMode.Open);
        var mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(image.InternalPath));
        
        return File(file, mimeType, Path.GetFileName(image.InternalPath));
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
}