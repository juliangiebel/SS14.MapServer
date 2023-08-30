using System.Diagnostics.CodeAnalysis;
using System.Web;
using MimeTypes;
using Quartz;
using SS14.MapServer.Configuration;
using SS14.MapServer.Helpers;
using SS14.MapServer.Jobs;
using SS14.MapServer.Models.Entities;
using SS14.MapServer.Services.Interfaces;

namespace SS14.MapServer.Services;

public sealed class FileUploadService
{
    private readonly FilePathsConfiguration _configuration = new();
    private readonly IJobSchedulingService _schedulingService;

    public FileUploadService(IConfiguration configuration, IJobSchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
        configuration.Bind(FilePathsConfiguration.Name, _configuration);
    }

    public async Task UploadImage(ImageFile image, IFormFile file, string? path = null)
    {
        if (ValidateImageFile(file, out var message))
            throw new ArgumentException(message);

        var filename = $"upload_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        path ??= Path.Combine(_configuration.ImagesPath, filename);

        await using var stream = File.Create(path);
        await file.CopyToAsync(stream);
        image.InternalPath = path;
    }

    public async Task UploadGridImages(string gitRef, Map map, IEnumerable<IFormFile> images)
    {
        var mapPath = Path.Combine(_configuration.GridImagesPath, gitRef, map.MapId);

        //Creates the maps image directory if it doesn't exist and clears it.
        Directory.CreateDirectory(mapPath).Clear();
        var grids = map.Grids.ToDictionary(grid => grid.GridId, grid => grid);

        foreach (var image in images)
        {
            var filename = Path.GetFileNameWithoutExtension(image.FileName);
            if  (!int.TryParse(filename, out var gridId))
                throw new ArgumentException($"One of the provided file names couldn't be parsed into a grid id: {HttpUtility.HtmlEncode(filename)}");

            if (!grids.TryGetValue(gridId, out var grid))
                throw new ArgumentException($"Grid id ${gridId} not present in map ${map.MapId}");

            string path;

            if (grid.Tiled)
            {
                path = await UploadAndProcessTiledImage(map.MapGuid, mapPath, gridId, image, grid.TileSize);
            }
            else
            {
                path = await UploadGridImage(mapPath, gridId, image);
            }

            grid.Path = path;
        }
    }

    public bool ValidateImageFile(IFormFile image, [NotNullWhen(true)] out string? message)
    {
        var extension = Path.GetExtension(image.FileName);
        if (!MimeTypeMap.TryGetMimeType(extension, out var mimeType))
        {
            message = $"Mime type not found for file extension {extension}";
            return true;
        }

        if (!_configuration.AllowedMimeTypes.Contains(mimeType))
        {
            message = $"Mime type not {mimeType} not allowed";
            return true;
        }

        message = null;
        return false;
    }

    private async Task<string> UploadGridImage(string mapPath, int gridId, IFormFile image)
    {
        if (ValidateImageFile(image, out var message))
            throw new ArgumentException(message);

        var name = $"{gridId}{Path.GetExtension(image.FileName)}";
        var path = Path.Combine(mapPath, name);

        await using var stream = File.Create(path);
        await image.CopyToAsync(stream);
        return path;
    }

    private async Task<string> UploadAndProcessTiledImage(Guid mapGuid, string mapPath, int gridId, IFormFile image, int tileSize)
    {
        if (ValidateImageFile(image, out var message))
            throw new ArgumentException(message);

        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid().ToString()}{Path.GetExtension(image.FileName)}");
        var targetPath = Path.Combine(mapPath, "tiles", gridId.ToString());

        await using var stream = File.Create(sourcePath);
        await image.CopyToAsync(stream);

        var processingOptions = new ProcessTiledImage.ProcessingOptions(mapGuid, gridId, sourcePath, targetPath, tileSize, true);
        var data = new JobDataMap
        {
            {ProcessTiledImage.ProcessOptionsKey, processingOptions}
        };

        await _schedulingService.RunJob<ProcessTiledImage>(
            $"{nameof(ProcessTiledImage)}-{mapGuid}-{gridId}",
            "Processing",
            data);

        return targetPath;
    }
}
