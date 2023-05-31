using LibGit2Sharp;
using LibGit2Sharp.Handlers;
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
    /// <param name="branch">[Optional] The branch to pull</param>
    /// <returns></returns>
    public string Sync(string workingDirectory, string? branch = null)
    {
        branch ??= _configuration.Branch;
        
        var repositoryName = Path.GetFileNameWithoutExtension(_configuration.RepositoryUrl);
        var repoDirectory = Path.Join(workingDirectory, repositoryName);

        if (!Path.IsPathRooted(repoDirectory))
            repoDirectory = Path.Join(Directory.GetCurrentDirectory(), repoDirectory);
        
       
        if (!Directory.Exists(repoDirectory))
            Clone(repoDirectory, branch);

        Pull(repoDirectory, branch);
  
        
        return repoDirectory;
    }

    private void Clone(string directory, string branch)
    {
        _log.Information("Cloning branch {Branch}...", branch);
        Repository.Clone(_configuration.RepositoryUrl, directory, new CloneOptions
        {
            RecurseSubmodules = true,
            BranchName = branch,
            OnProgress = LogProgress,
            OnCheckoutProgress = (progress, _, _) => LogProgress(progress)
            
        });
        _log.Information("Done cloning");
    }
    
    private void Pull(string repoDirectory, string branch)
    {
        _log.Information( "Pulling branch {Branch}...", branch);
        
        using var repository = new Repository(repoDirectory);
        Commands.Checkout(repository, branch);
        var signature = repository.Config.BuildSignature(DateTimeOffset.Now);

        var pullOptions = new PullOptions
        {
            FetchOptions = new FetchOptions
            {
                OnProgress = LogProgress
            }
        };

        Commands.Pull(repository, signature, pullOptions);

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