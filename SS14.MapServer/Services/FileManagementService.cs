using SS14.MapServer.Configuration;

namespace SS14.MapServer.Services;

public sealed class FileManagementService
{
    private readonly FilePathsConfiguration _pathsConfiguration = new();
    private readonly ProcessingConfiguration _processingConfiguration = new();


    public FileManagementService(IConfiguration configuration)
    {
        configuration.Bind(FilePathsConfiguration.Name, _pathsConfiguration);
        configuration.Bind(ProcessingConfiguration.Name, _processingConfiguration);
    }

    /**
     * Returns the sum of the size of all files in the given directory
     */
    public async ValueTask<long> GetDirectoryFilesSize(string path)
    {
        var directory = new DirectoryInfo(path);
        if (!directory.Exists)
            return 0;

        return directory.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
    }

    public async ValueTask<long> GetImageDirectoriesFilesSize()
    {
        var usage = await GetDirectoryFilesSize(_pathsConfiguration.GridImagesPath);
        usage += await GetDirectoryFilesSize(_pathsConfiguration.ImagesPath);
        usage += await GetDirectoryFilesSize(_pathsConfiguration.TilesPath);
        return usage;
    }

    public async ValueTask<long> GetBuildDirectoryFilesSize()
    {
        return await GetDirectoryFilesSize(_processingConfiguration.TargetDirectory);
    }

    public async Task<int> CleanBuildDirectories()
    {
        return await Task.Run(InternalCleanBuildDirectories);
    }

    private Task<int> InternalCleanBuildDirectories()
    {
        var filesCounter = 0;
        var poolDirectory = new DirectoryInfo(_processingConfiguration.TargetDirectory);
        foreach (var directory in poolDirectory.GetDirectories())
        {
            var files = _processingConfiguration.JunkFilePatterns
                .SelectMany(pattern => directory.EnumerateFiles(pattern, SearchOption.AllDirectories));

            foreach (var file in files)
            {
                try
                {
                    file.Delete();
                    filesCounter++;
                }
                catch (Exception e)
                {
                    // Ignore
                }
            }
        }

        return Task.FromResult(filesCounter);
    }
}
