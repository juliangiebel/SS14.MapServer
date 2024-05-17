using Serilog;
using SS14.BuildRunner.BuildRunners;
using SS14.BuildRunner.Configuration;
using SS14.BuildRunner.Services;
using SS14.MapServer.Configuration;

namespace SS14.MapServer.Services;

public sealed class StartupCheckService
{
    private readonly FilePathsConfiguration _filePathsConfiguration = new();
    private readonly BuildConfiguration _buildConfiguration = new();
    private readonly GitConfiguration _gitConfiguration = new();
    private readonly ProcessingConfiguration _processingConfiguration = new();
    private readonly GitService _gitService;
    private readonly ContainerService _containerService;
    private readonly LocalBuildService _localBuildService;


    public StartupCheckService(ContainerService containerService, IConfiguration configuration, LocalBuildService localBuildService, GitService gitService)
    {
        _containerService = containerService;
        _localBuildService = localBuildService;
        _gitService = gitService;
        configuration.Bind(FilePathsConfiguration.Name, _filePathsConfiguration);
        configuration.Bind(BuildConfiguration.Name, _buildConfiguration);
        configuration.Bind(GitConfiguration.Name, _gitConfiguration);
        configuration.Bind(ProcessingConfiguration.Name, _processingConfiguration);
    }

    public async Task<bool> RunStartupCheck()
    {
        var configuration = CheckConfiguration();
        var buildRunner = await CheckBuildServices();

        return configuration & buildRunner;
    }

    private bool CheckConfiguration()
    {
        Log.Information("Checking configuration:");

        var result = true;

        if (_buildConfiguration.Enabled)
        {
            if (_gitConfiguration.RepositoryUrl == string.Empty)
            {
                Log.Error(" - Git.RepositoryUrl is not set");
                result = false;
            }

            if (!Directory.Exists(_processingConfiguration.TargetDirectory))
            {
                Log.Error(" - Processing.TargetDirectory doesn't exist: {TargetDir}", _processingConfiguration.TargetDirectory);
                result = false;
            }
        }

        if (result)
            Log.Information(" - Configuration is valid");

        return result;
    }

    private async Task<bool> CheckBuildServices()
    {
        Log.Information("Automatic build features are {BuildFeaturesStatus}", _buildConfiguration.Enabled ? "Enabled" : "Disabled");
        if (!_buildConfiguration.Enabled)
            return true;

        var result = await CheckGitPrerequisites();

        return result && _buildConfiguration.Runner switch
        {
            BuildRunnerName.Local => await CheckLocalRunnerPrerequisites(),
            BuildRunnerName.Container => await CheckContainerPrerequisites(),
            _ => false
        };
    }

    private async Task<bool> CheckGitPrerequisites()
    {
        Log.Information("Checking Git:");

        try
        {
            var version = await _gitService.GetGitVersion();
            Log.Information(" - Git version: {GitVersion}", version.Replace("\n", ""));
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Couldn't determine Git version");
            return false;
        }

        return true;
    }

    private async Task<bool> CheckContainerPrerequisites()
    {
        Log.Information("Checking container manager:");

        try
        {
            var info = await _containerService.GetSystemInformation();
            Log.Information(" - Container manager found: {ManagerName}", info.Name);
            Log.Information(" - Version: {ManagerVersion}", info.ServerVersion);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Couldn't connect to a supported container manager");
            return false;
        }

        return true;
    }

    private async Task<bool> CheckLocalRunnerPrerequisites()
    {
        Log.Information("Checking DotNet:");

        try
        {
            var version = await _localBuildService.GetDotNetVersion();
            Log.Information(" - DotNet version: {DotNetVersion}", version.Replace("\n", ""));
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Couldn't determine dotnet version");
            return false;
        }

        return true;
    }
}
