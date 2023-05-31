using SS14.MapServer.Helpers;
using SS14.MapServer.MapProcessing.Services;
using SS14.MapServer.Services;

namespace SS14.MapServer.Tests;

public class ImageProcessingServiceTests
{
    private const string ResourcePath = "resources\\tiling_test_source.png";
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
        var tiles = await _processingService!.TileImage("test", 0, _sourcePath!, _targetPath!, 512);
        
        Assert.Multiple(() =>
        {
            Assert.That(tiles, Has.Count.EqualTo(60));
            Assert.That(File.Exists(tiles[0].Path), Is.True);
        });
    }
}