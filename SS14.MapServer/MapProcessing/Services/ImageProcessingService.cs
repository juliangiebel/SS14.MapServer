using SixLabors.ImageSharp.Advanced;
using SS14.MapServer.Models.Entities;

namespace SS14.MapServer.MapProcessing.Services;

public sealed class ImageProcessingService
{
    private const int MinTileSize = 256;
    
    public async Task<List<Tile>> TileImage(string mapId, int gridId, string sourcePath, string targetPath, int tileSize)
    {
        if (tileSize < MinTileSize)
            throw new ArgumentOutOfRangeException($"Provider tile size {tileSize} is smaller than minimum tile size {MinTileSize}");
        
        if (!Path.HasExtension(sourcePath))
            throw new Exception($"Invalid image path: {sourcePath}");

        Directory.CreateDirectory(targetPath);
        
        var file = File.Open(sourcePath, FileMode.Open);
        using var image = await Image.LoadAsync(file);
        
        var bounds = image.Bounds;
        var heightSteps = bounds.Height / tileSize;
        var widthSteps = bounds.Width / tileSize;

        var tiles = new List<Tile>();
        var extension = Path.GetExtension(sourcePath);
        var encoder = image.DetectEncoder(sourcePath);

        for (var y = 0; y < heightSteps; y++)
        {
            for (var x = 0; x < widthSteps; x++)
            {
                var rectangle = new Rectangle(tileSize * x, tileSize * y, tileSize, tileSize);
                var tile = image.Clone(x => x.Crop(rectangle));
                
                var path = Path.Combine(targetPath, $"tile_{Guid.NewGuid()}{extension}");
                await tile.SaveAsync(path, encoder);
                
                tiles.Add(new Tile(mapId, gridId, x, y, tileSize, path));
            }   
        }

        return tiles;
    }
}