using System.Web;
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
    
    public async Task UploadGridImages(Map map, IList<IFormFile> images)
    {
        var mapPath = Path.Combine(_configuration.GridImagesPath, map.Id);
            
        //Creates the maps image directory if it doesn't exist and clears it.
        Directory.CreateDirectory(mapPath).Clear();
        var grids = map.Grids.ToDictionary(grid => grid.GridId, grid => grid); 

        foreach (var image in images)
        {
            var filename = Path.GetFileNameWithoutExtension(image.FileName);
            if  (!int.TryParse(filename, out var gridId))
                throw new ArgumentException($"One of the provided file names couldn't be parsed into a grid id: {HttpUtility.HtmlEncode(filename)}");

            if (!grids.TryGetValue(gridId, out var grid))
                throw new ArgumentException($"Grid id ${gridId} not present in map ${map.Id}");

            string path;
            
            if (grid.Tiled)
            {
                path = await UploadAndProcessTiledImage(map.Id, mapPath, gridId, image, grid.TileSize);
            }
            else
            {
                path = await UploadImage(mapPath, gridId, image);
            }
            
            grid.Path = path;
        }
    }

    private async Task<string> UploadImage(string mapPath, int gridId, IFormFile image)
    {
        var name = $"{gridId}{Path.GetExtension(image.FileName)}";
        var path = Path.Combine(mapPath, name);

        await using var stream = File.Create(path);
        await image.CopyToAsync(stream);
        return path;
    }

    private async Task<string> UploadAndProcessTiledImage(string mapId, string mapPath, int gridId, IFormFile image, int tileSize)
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid().ToString()}{Path.GetExtension(image.FileName)}");
        var targetPath = Path.Combine(mapPath, "tiles", gridId.ToString());
        
        await using var stream = File.Create(sourcePath);
        await image.CopyToAsync(stream);

        var processingOptions = new ProcessTiledImage.ProcessingOptions(mapId, gridId, sourcePath, targetPath, tileSize, true);
        var data = new JobDataMap
        {
            {ProcessTiledImage.ProcessOptionsKey, processingOptions}
        };

        await _schedulingService.RunJob<ProcessTiledImage>(nameof(ProcessTiledImage), "Processing", data);
        
        return targetPath;
    }
}