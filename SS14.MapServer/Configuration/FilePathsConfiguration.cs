namespace SS14.MapServer.Configuration;

public sealed class FilePathsConfiguration
{
    public const string Name = "FilePaths";

    public string GridImagesPath { get; set; } = "data/grid_images";
    public string ImagesPath { get; set; } = "data/images";
    public string TilesPath { get; set; } = "data/tiles";
    public string TemporaryFilesPath { get; set; } = "data/tmp";

    public List<string> AllowedMimeTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];
}
