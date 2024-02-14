using System.Collections.Immutable;
using Microsoft.Extensions.FileSystemGlobbing;
using SS14.MapServer.BuildRunners;
using SS14.MapServer.Configuration;
using SS14.MapServer.Services;
using SS14.MapServer.Services.Interfaces;

namespace SS14.MapServer.MapProcessing.Services;

public sealed class MapUpdateService
{
    private readonly BuildConfiguration _buildConfiguration = new();
    private readonly GitConfiguration _gitConfiguration = new();
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
        configuration.Bind(GitConfiguration.Name, _gitConfiguration);
    }

    /// <summary>
    /// Pulls the latest git commit, builds and runs the map renderer and imports the generated maps.
    /// </summary>
    /// <param name="directory">The directory to operate in</param>
    /// <param name="gitRef">The git ref to pull (branch/commit)</param>
    /// <param name="maps">A list of map file names to be generated</param>
    /// <param name="repositoryUrl">The clone url of the repository to clone from</param>
    /// <param name="syncAll">Ignore the maps parameter and update all maps</param>
    /// <param name="forceTiled">Force the map to be tiled after rendering</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <returns>The commit that was checked out for building and running the map renderer</returns>
    /// <remarks>
    /// Syncing the maps doesn't create a new working directory so running this in parallel on the same directory would cause errors.<br/>
    /// </remarks>
    public async Task<MapProcessResult> UpdateMapsFromGit(string directory,
        string gitRef,
        IEnumerable<string> maps,
        string? repositoryUrl = null,
        bool syncAll = false,
        bool forceTiled = false,
        CancellationToken cancellationToken = default)
    {
        var workingDirectory = _gitService.Sync(directory, gitRef, repositoryUrl);
        var strippedGitRef = GitService.StripRef(gitRef);

        if (syncAll)
        {
            var files = _gitConfiguration.MapFilePatterns
                .Select(pattern => Directory.GetFiles(workingDirectory, pattern))
                .SelectMany(x => x)
                .Select(file => Path.GetRelativePath(workingDirectory, file));

            //Check for map files
            var mapFileMatcher = new Matcher();
            mapFileMatcher.AddIncludePatterns(_gitConfiguration.MapFilePatterns);
            mapFileMatcher.AddExcludePatterns(_gitConfiguration.MapFileExcludePatterns);
            var mapFileMatchResult = mapFileMatcher.Match(files);

            if (!mapFileMatchResult.HasMatches)
                return new MapProcessResult(strippedGitRef, ImmutableList<Guid>.Empty);

            maps = mapFileMatchResult.Files.Select(file => Path.GetFileName(file.Path));
        }

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
            BuildRunnerName.Local => await _localBuildService.BuildAndRun(workingDirectory, command, args, cancellationToken),
            BuildRunnerName.Container => await _containerService.BuildAndRun(workingDirectory, command, args, cancellationToken),
            _ => throw new ArgumentOutOfRangeException()
        };

        var mapIds = await _mapReaderService.UpdateMapsFromFs(path,  strippedGitRef, forceTiled, cancellationToken);
        return new MapProcessResult(strippedGitRef, mapIds);
    }
}
