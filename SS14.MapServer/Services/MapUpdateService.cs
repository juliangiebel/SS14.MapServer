using SS14.MapServer.Configuration;
using SS14.MapServer.Services.Interfaces;

namespace SS14.MapServer.Services;

public sealed class MapUpdateService
{
    private static readonly Semaphore SyncSemaphore = new(1, 1);
    
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
    /// <param name="maps">A list of map file names to be generated</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <remarks>
    /// Syncing the maps doesn't create a new working directory so running this in parallel would cause errors.<br/>
    /// That's why this method is protected by a semaphore which prevents it from being run in parallel.
    /// </remarks>
    public async Task UpdateMapsFromGit(IEnumerable<string> maps)
    {
        SyncSemaphore.WaitOne(TimeSpan.FromMinutes(_buildConfiguration.ProcessTimeoutMinutes));
        var workingDirectory = _gitService.Sync();
        
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

        SyncSemaphore.Release();
    }

    public async Task<List<string>> GetChangedMaps()
    {
        return new List<string>();
    }
}