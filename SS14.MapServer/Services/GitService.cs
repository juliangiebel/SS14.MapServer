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
    public string Sync(string workingDirectory, string? gitRef = null)
    {
        gitRef ??= _configuration.Branch;

        var repositoryName = Path.GetFileNameWithoutExtension(_configuration.RepositoryUrl);
        var repoDirectory = Path.Join(workingDirectory, repositoryName);

        if (!Path.IsPathRooted(repoDirectory))
            repoDirectory = Path.Join(Directory.GetCurrentDirectory(), repoDirectory);


        if (!Directory.Exists(repoDirectory))
            Clone(repoDirectory, gitRef);

        Pull(repoDirectory, gitRef);


        return repoDirectory;
    }

    private void Clone(string directory, string gitRef)
    {
        _log.Information("Cloning branch/commit {Ref}...", gitRef);
        var repoDirectory = Repository.Clone(_configuration.RepositoryUrl, directory, new CloneOptions
        {
            RecurseSubmodules = true,
            OnProgress = LogProgress,
            OnCheckoutProgress = (_, completed, total) => LogDownloadProgress(completed, total)
        });

        using var repository = new Repository(repoDirectory);
        Commands.Checkout(repository, gitRef);
        _log.Information("Done cloning");
    }

    private void LogDownloadProgress(int completedSteps, int totalSteps)
    {
        if (completedSteps == 0)
            return;

        var percentage =  completedSteps / totalSteps * 100;

        if (percentage % 10 != 0)
            return;

        _log.Verbose("Progress: {Percentage}%", percentage);
    }

    private void Pull(string repoDirectory, string gitRef)
    {
        _log.Information( "Pulling branch/commit {Ref}...", gitRef);

        using var repository = new Repository(repoDirectory);
        Commands.Checkout(repository, gitRef);
        var signature = repository.Config.BuildSignature(DateTimeOffset.Now);

        var pullOptions = new PullOptions
        {
            FetchOptions = new FetchOptions
            {
                OnProgress = LogProgress,
                OnTransferProgress = progress =>
                {
                    LogDownloadProgress(progress.ReceivedObjects, progress.TotalObjects);
                    return true;
                }
            }
        };

        Commands.Pull(repository, signature, pullOptions);

        foreach (var submodule in repository.Submodules)
        {
            repository.Submodules.Update(submodule.Name, new SubmoduleUpdateOptions
            {
                OnProgress = LogProgress,
                OnTransferProgress = progress =>
                {
                    LogDownloadProgress(progress.ReceivedObjects, progress.TotalObjects);
                    return true;
                }
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
