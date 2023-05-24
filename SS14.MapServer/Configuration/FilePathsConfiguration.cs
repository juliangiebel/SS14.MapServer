namespace SS14.MapServer.Configuration;

public sealed class FilePathsConfiguration
{
    public const string Name = "FilePaths";
    
    public string GridImagesPath { get; set; } = null!;
    public string ImagesPath { get; set; } = null!;
    public string TilesPath { get; set; } = null!;
    public string TemporaryFilesPath { get; set; } = null!;

    public List<string> AllowedMimeTypes { get; set; } = new()
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };
}