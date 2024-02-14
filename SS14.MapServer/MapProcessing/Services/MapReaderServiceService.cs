using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SS14.MapServer.Configuration;
using SS14.MapServer.Helpers;
using SS14.MapServer.Models;
using SS14.MapServer.Models.DTOs;
using SS14.MapServer.Models.Entities;
using SS14.MapServer.Services;
using SS14.MapServer.Services.Interfaces;

namespace SS14.MapServer.MapProcessing.Services;

public sealed class MapReaderServiceService : IMapReaderService
{
    private readonly BuildConfiguration _buildConfiguration = new();
    private readonly GitConfiguration _gitConfiguration = new();
    private readonly FileUploadService _fileUploadService;
    private readonly Context _context;

    public MapReaderServiceService(IConfiguration configuration, FileUploadService fileUploadService, Context context)
    {
        _fileUploadService = fileUploadService;
        _context = context;

        configuration.Bind(BuildConfiguration.Name, _buildConfiguration);
        configuration.Bind(GitConfiguration.Name, _gitConfiguration);
    }

    public async Task<IList<Guid>> UpdateMapsFromFs(string path, string gitRef, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Map import path not found: {path}");

        var ids = new List<Guid>();
        var directories = Directory.EnumerateDirectories(path);
        foreach (var directory in directories)
        {
            var dataFilePath = Path.Combine(directory, _buildConfiguration.MapDataFileName);
            if (!File.Exists(dataFilePath))
                continue;

            string rawJson;

            using (var reader = new StreamReader(dataFilePath))
            {
                rawJson = await reader.ReadToEndAsync(cancellationToken);
            }

            var converter = new MapDataAreaConverter();
            var data = JsonConvert.DeserializeObject<MapRendererData>(rawJson, converter);
            if (data == null)
                continue;

            var newMap = !await _context.Map!.AnyAsync(e => e.GitRef == gitRef && e.MapId == data.Id.ToLower(), cancellationToken);
            Map map;

            if (newMap)
            {
                map = new Map();
            }
            else
            {
                map = await _context.Map!
                    .Include(e => e.Grids)
                    .SingleAsync(e => e.GitRef == gitRef && e.MapId == data.Id.ToLower(), cancellationToken);
            }

            map.GitRef = gitRef;
            map.MapId = data.Id.ToLower();
            map.DisplayName = data.DisplayName;
            map.Attribution = data.Attribution;
            map.ParallaxLayers = data.ParallaxLayers;

            if (newMap)
                await _context.Map!.AddAsync(map, cancellationToken);

            //Remove previous grids if there are any
            if (map.Grids.Count > 0)
                _context.RemoveRange(map.Grids);

            map.Grids.Clear();

            var streams = new List<FileStream>();
            var gridImages = new List<IFormFile>();

            foreach (var gridData in data.Grids)
            {
                var imagePath = Path.Join(path, gridData.Path);
                var file = File.OpenRead(imagePath);
                var fileName = $"{gridData.GridId}{Path.GetExtension(file.Name)}";
                var formFile = new FormFile(file, 0, file.Length, fileName, fileName);
                gridImages.Add(formFile);
                streams.Add(file);

                var grid = new Grid
                {
                    Id = Guid.NewGuid(),
                    GridId = gridData.GridId,
                    Extent = gridData.Extent,
                    Offset = gridData.Offset,
                    //Only tile maps used by the viewer and prevent small grids from being tiled
                    Tiled = gridData.Extent.GetArea() >= 65536 && gitRef == _gitConfiguration.Branch
                };
                map.Grids.Add(grid);
                _context.Add(grid);
            }

            await _fileUploadService.UploadGridImages(gitRef, map, gridImages);

            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }

            ids.Add(map.MapGuid);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (_buildConfiguration.CleanMapFolderAfterImport)
            new DirectoryInfo(path).Clear();

        return ids;
    }
}
