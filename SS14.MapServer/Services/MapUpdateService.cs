using SS14.MapServer.Configuration;
using SS14.MapServer.Services.Interfaces;

namespace SS14.MapServer.Services;

public sealed class MapUpdateService
{
    private readonly BuildConfiguration _buildConfiguration = new();
    private readonly GitService _gitService;
    private readonly LocalBuildService _localBuildService;
    private readonly ContainerService _containerService;
    private readonly IMapReaderService _mapReaderService;

    public MapUpdateService(
        IConfiguration configuration, 
        GitService gitService, 
        LocalBuildService localBuildService, 
        ContainerService containerService, 
        IMapReaderService mapReaderService)
    {
        _gitService = gitService;
        _localBuildService = localBuildService;
        _containerService = containerService;
        _mapReaderService = mapReaderService;
        configuration.Bind(BuildConfiguration.Name, _buildConfiguration);
    }

    /// <summary>
    /// Pulls the latest git commit, builds and runs the map renderer and imports the generated maps.
    /// </summary>
    /// <param name="directory">The directory to operate in</param>
    /// <param name="maps">A list of map file names to be generated</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <returns>The commit that was checked out for building and running the map renderer</returns>
    /// <remarks>
    /// Syncing the maps doesn't create a new working directory so running this in parallel on the same directory would cause errors.<br/>
    /// </remarks>
    public async Task<string> UpdateMapsFromGit(string directory, IEnumerable<string> maps)
    {
        var workingDirectory = _gitService.Sync(directory);

        var command = Path.Join(
            _buildConfiguration.RelativeOutputPath,
            _buildConfiguration.MapRendererProjectName,
            _buildConfiguration.MapRendererCommand
        );

        var args = new List<string>
        {
            _buildConfiguration.MapRendererOptionsString
        };

        args.AddRange(maps);

        var path = _buildConfiguration.Runner switch
        {
            BuildRunnerName.Local => await _localBuildService.BuildAndRun(workingDirectory, command, args),
            BuildRunnerName.Container => await _containerService.BuildAndRun(workingDirectory, command, args),
            _ => throw new ArgumentOutOfRangeException()
        };

        await _mapReaderService.UpdateMapsFromFS(path);

        return _gitService.GetRepoCommitHash(workingDirectory);
    }

    public async Task<List<string>> GetChangedMaps()
    {
        return new List<string>();
    }
}