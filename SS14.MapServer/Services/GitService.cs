using LibGit2Sharp;
using Serilog;
using SS14.MapServer.Configuration;
using ILogger = Serilog.ILogger;

namespace SS14.MapServer.Services;

public sealed class GitService
{
    private readonly GitConfiguration _configuration = new();
    private readonly ILogger _log;

    public GitService(IConfiguration configuration)
    {
        configuration.Bind(GitConfiguration.Name, _configuration);
        _log = Log.ForContext(typeof(GitService));
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="workingDirectory"></param>
    /// <param name="gitRef">[Optional] The Ref to pull</param>
    /// <returns></returns>
    public string Sync(string workingDirectory, string? gitRef = null, string? repoUrl = null)
    {
        gitRef ??= _configuration.Branch;
        repoUrl ??= _configuration.RepositoryUrl;

        var repositoryName = Path.GetFileNameWithoutExtension(repoUrl);
        var repoDirectory = Path.Join(workingDirectory, repositoryName);

        if (!Path.IsPathRooted(repoDirectory))
            repoDirectory = Path.Join(Directory.GetCurrentDirectory(), repoDirectory);


        if (!Directory.Exists(repoDirectory))
            Clone(repoUrl, repoDirectory, gitRef);

        Pull(repoDirectory, gitRef);


        return repoDirectory;
    }

    private void Clone(string repoUrl, string directory, string gitRef)
    {
        _log.Information("Cloning branch/commit {Ref}...", gitRef);
        var repoDirectory = Repository.Clone(repoUrl, directory, new CloneOptions
        {
            RecurseSubmodules = true,
            OnProgress = LogProgress
        });

        using var repository = new Repository(repoDirectory);
        Commands.Checkout(repository, gitRef);
        _log.Information("Done cloning");
    }

    private void Pull(string repoDirectory, string gitRef)
    {
        _log.Information( "Pulling branch/commit {Ref}...", gitRef);
        _log.Debug("Opening repository in: {RepositoryPath}", repoDirectory);

        using var repository = new Repository(repoDirectory);
        _log.Debug("Fetching ref");

        Commands.Fetch(
            repository,
            "origin",
            new []{gitRef},
            new FetchOptions
            {
                OnProgress = LogProgress
            },
            "Fetched ref");

        _log.Debug("Checking out {Ref}", gitRef);
        Commands.Checkout(repository, gitRef);
        var signature = repository.Config.BuildSignature(DateTimeOffset.Now);

        var pullOptions = new PullOptions
        {
            FetchOptions = new FetchOptions
            {
                OnProgress = LogProgress
            }
        };
        _log.Debug("Pulling latest changes");
        Commands.Pull(repository, signature, pullOptions);

        _log.Debug("Updating submodules");
        foreach (var submodule in repository.Submodules)
        {
            repository.Submodules.Update(submodule.Name, new SubmoduleUpdateOptions
            {
                OnProgress = LogProgress
            });
        }

        _log.Information("Done pulling");
    }

    private bool LogProgress(string? progress)
    {
        _log.Verbose("Progress: {Progress}", progress);
        return true;
    }

    /// <summary>
    /// Returns the commit hash the repo contained in the given directory is on
    /// </summary>
    /// <param name="directory">A directory containing a git repository</param>
    /// <returns>The commit has of the repository</returns>
    /// <exception cref="NotImplementedException"></exception>
    public string GetRepoCommitHash(string directory)
    {
        using var repository = new Repository(directory);
        return repository.Head.Tip.Sha;
    }
}
