using SixLabors.ImageSharp;
using SS14.MapServer.Helpers;
using SS14.MapServer.MapProcessing.Services;
using SS14.MapServer.Services;

namespace SS14.MapServer.Tests;

public class ImageProcessingServiceTests
{
    private const string ResourcePath = "resources/saltern.webp";
    private const string OutputPath = "test";

    private ImageProcessingService? _processingService;

    private string? _sourcePath;
    private string? _targetPath;

    [SetUp]
    public void Setup()
    {
        var directory = Directory.GetCurrentDirectory();
        _sourcePath = Path.Combine(directory, ResourcePath);
        _targetPath = Path.Combine(directory, OutputPath);

        if (Directory.Exists(_targetPath))
        {
            var targetDir = new DirectoryInfo(_targetPath);
            targetDir.Clear();
        }

        _processingService = new ImageProcessingService();
    }

    [Test]
    public async Task ImageTilingTest()
    {
        var tiles = await _processingService!.TileImage(Guid.NewGuid(), 0, _sourcePath!, _targetPath!, 256);

        Assert.Multiple(() =>
        {
            Assert.That(tiles, Has.Count.EqualTo(198));
            Assert.That(File.Exists(tiles[0].Path), Is.True);
            // Also check that the edge tiles are properly sized
            Assert.That(Image.Load(tiles.First(t => t.Y == 10).Path).Height, Is.EqualTo(256));
            Assert.That(Image.Load(tiles.First(t => t.X == 17).Path).Width, Is.EqualTo(256));
        });
    }
}
