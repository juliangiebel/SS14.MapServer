using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using SS14.MapServer.Models.Entities;

namespace SS14.MapServer.MapProcessing.Services;

public sealed class ImageProcessingService
{
    private const int MinTileSize = 256;

    public async Task<List<Tile>> TileImage(Guid mapGuid, int gridId, string sourcePath, string targetPath, int tileSize)
    {
        if (tileSize < MinTileSize)
            throw new ArgumentOutOfRangeException($"Provided tile size {tileSize} is smaller than minimum tile size {MinTileSize}");

        if (!Path.HasExtension(sourcePath))
            throw new Exception($"Invalid image path: {sourcePath}");

        Directory.CreateDirectory(targetPath);

        var file = File.Open(sourcePath, FileMode.Open);
        using var image = await Image.LoadAsync(file);

        var bounds = image.Bounds;
        var heightSteps = Math.Ceiling((double) bounds.Height / tileSize);
        var widthSteps = Math.Ceiling((double) bounds.Width / tileSize);

        var tiles = new List<Tile>();
        var extension = Path.GetExtension(sourcePath);
        var encoder = image.DetectEncoder(sourcePath);

        var compressedWebpEncoder = new WebpEncoder
        {
            Method = WebpEncodingMethod.Level6,
            FileFormat = WebpFileFormatType.Lossy,
            Quality = 1,
            SkipMetadata = true,
            FilterStrength = 0,
            TransparentColorMode = WebpTransparentColorMode.Preserve
        };

        for (var y = 0; y < heightSteps; y++)
        {
            for (var x = 0; x < widthSteps; x++)
            {
                var rectangle = new Rectangle(tileSize * x, tileSize * y, tileSize, tileSize);
                rectangle.Intersect(bounds);

                var tile = image.Clone(img => img.Crop(rectangle));
                // Make sure to pad the edge tiles to full size
                if (x == widthSteps - 1 || y == heightSteps - 1)
                    tile.Mutate(img => img.Resize(new ResizeOptions
                    {
                        Size = new Size(tileSize, tileSize),
                        Mode = ResizeMode.Pad,
                        Sampler = KnownResamplers.NearestNeighbor,
                        PadColor = default,
                        Position = AnchorPositionMode.TopLeft
                    }));
                var preview = tile.Clone(img => img.Pixelate(8));

                var path = Path.Combine(targetPath, $"tile_{Guid.NewGuid()}{extension}");
                await tile.SaveAsync(path, encoder);

                using var stream = new MemoryStream();
                await preview.SaveAsync(stream, compressedWebpEncoder);
                var previewData = stream.ToArray();

                tiles.Add(new Tile(mapGuid, gridId, x, y, tileSize, path, previewData));
            }
        }

        return tiles;
    }
}
