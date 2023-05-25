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

    public async Task UpdateMapsFromGit(List<string> maps)
    {
        var workingDirectory = await _gitService.Sync();
        var mapRendererCommand = new List<string>
        {
            "maprenderer"
        };
        
        var path = _buildConfiguration.Runner switch
        {
            BuildRunnerName.Local => await _localBuildService.BuildAndRun(workingDirectory, mapRendererCommand),
            BuildRunnerName.Container => await _containerService.BuildAndRun(workingDirectory, mapRendererCommand),
            _ => throw new ArgumentOutOfRangeException()
        };
        
    }

    public async Task<List<string>> GetChangedMaps()
    {
        return new List<string>();
    }
}