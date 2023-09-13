﻿using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Serilog;
using SS14.MapServer.BuildRunners;
using SS14.MapServer.Configuration;
using ILogger = Serilog.ILogger;

namespace SS14.MapServer.Services;

public sealed class GitService
{
    private readonly LocalBuildService _buildService;

    private readonly GitConfiguration _configuration = new();
    private readonly ILogger _log;

    public GitService(IConfiguration configuration, LocalBuildService buildService)
    {
        _buildService = buildService;
        configuration.Bind(GitConfiguration.Name, _configuration);
        _log = Log.ForContext(typeof(GitService));
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="workingDirectory"></param>
    /// <param name="gitRef">[Optional] The Ref to pull</param>
    /// <param name="repoUrl"></param>
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

    public static string StripRef(string gitRef)
    {
        return gitRef.Split(":").Last();
    }

    private void Clone(string repoUrl, string directory, string gitRef)
    {
        _log.Information("Cloning branch/commit {Ref}...", gitRef);


        /*var repoDirectory = Repository.Clone(repoUrl, directory, new CloneOptions
        {
            RecurseSubmodules = true,
            OnProgress = LogProgress
        });

        using var repository = new Repository(repoDirectory);

        Commands.Fetch(
            repository,
            "origin",
            new []{gitRef},
            new FetchOptions
            {
                OnProgress = LogProgress
            },
            null);

        Commands.Checkout(repository, StripRef(gitRef));*/
        _log.Information("Done cloning");
    }

    private void Pull(string repoDirectory, string gitRef)
    {
        _log.Information( "Pulling branch/commit {Ref}...", gitRef);
        _log.Debug("Opening repository in: {RepositoryPath}", repoDirectory);

        using var repository = new Repository(repoDirectory);
        //Set an identity
        _log.Debug("Setting identity");
        repository.Config.Set("user.name", _configuration.Identity.Name);
        repository.Config.Set("user.email", _configuration.Identity.Email);

        if (_configuration.SshCommand != null)
        {
            _log.Debug("Setting ssh command");
            repository.Config.Set("core.sshcommand", _configuration.SshCommand);
        }

        _log.Debug("Fetching ref");
        _buildService.Run(repoDirectory, "git", new List<string> { "fetch -fu origin", gitRef }).Wait();

        _log.Debug("Checking out {Ref}", StripRef(gitRef));
        Commands.Checkout(repository, StripRef(gitRef));

        _log.Debug("Pulling latest changes");
        _buildService.Run(repoDirectory, "git", new List<string> { "pull origin HEAD --ff-only" }).Wait();

        _log.Debug("Updating submodules");
        foreach (var submodule in repository.Submodules)
        {
            if (submodule.UpdateRule == SubmoduleUpdate.None)
                continue;

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
}
