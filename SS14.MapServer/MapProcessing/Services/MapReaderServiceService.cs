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
    private readonly FileUploadService _fileUploadService;
    private readonly Context _context;

    public MapReaderServiceService(IConfiguration configuration, FileUploadService fileUploadService, Context context)
    {
        _fileUploadService = fileUploadService;
        _context = context;

        configuration.Bind(BuildConfiguration.Name, _buildConfiguration);
    }

    public async Task<IList<Guid>> UpdateMapsFromFs(string path, string gitRef = "master", CancellationToken cancellationToken = default)
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

            var map = await _context.Maps?
                .Include(e => e.Grids)
                .SingleOrDefaultAsync(e => e.GitRef == gitRef && e.MapId == data.Id, cancellationToken)!;

            var newMap = false;

            if (map == default)
            {
                map = new Map();
                newMap = true;
            }

            map.GitRef = gitRef;
            map.MapId = data.Id;
            map.DisplayName = data.DisplayName;
            map.Attribution = data.Attribution;
            map.ParallaxLayers = data.ParallaxLayers;

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
                    Tiled = gridData.Tiled,
                };
                map.Grids.Add(grid);
                _context.Add(grid);
            }

            await _fileUploadService.UploadGridImages(gitRef, map, gridImages);

            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }

            if (newMap)
            {
                var id = (await _context.Maps.AddAsync(map, cancellationToken)).Entity.MapGuid;
                ids.Add(id);
            }
            else
            {
                ids.Add(map.MapGuid);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        if (_buildConfiguration.CleanMapFolderAfterImport)
            new DirectoryInfo(path).Clear();

        return ids;
    }
}
