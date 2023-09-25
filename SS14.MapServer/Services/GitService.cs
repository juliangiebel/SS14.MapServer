using System.Diagnostics;
using Serilog;
using SS14.MapServer.BuildRunners;
using SS14.MapServer.Configuration;
using ILogger = Serilog.ILogger;

namespace SS14.MapServer.Services;

public sealed class GitService
{
    private const string GitCommand = "git";

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
    /// Gets the git version. Used for determining it git is installed
    /// </summary>
    public async Task<string> GetGitVersion(CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.FileName = GitCommand;
        process.StartInfo.Arguments = "--version";
        process.Start();
        await process.WaitForExitAsync(cancellationToken);
        return await process.StandardOutput.ReadToEndAsync(cancellationToken);
    }

    /// <summary>
    /// Clones the repo if it hasn't been cloned yet and pulls the provided git ref or the current branch
    /// </summary>
    /// <param name="workingDirectory"></param>
    /// <param name="gitRef">[Optional] The Ref to pull</param>
    /// <param name="repoUrl"></param>
    /// <returns>The directory the repository is in</returns>
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
        var task = _buildService.Run(directory, GitCommand, new List<string> { "rev-parse HEAD" });
        task.Wait();
        return task.Result;
    }

    public static string StripRef(string gitRef)
    {
        return gitRef.Split(":").Last();
    }

    private void Clone(string repoUrl, string directory, string gitRef)
    {
        _log.Information("Cloning branch/commit {Ref}...", gitRef);

        var sshConfig = _configuration.SshCommand != null
            ? $"--config core.sshcommand=\"{_configuration.SshCommand}\""
            : "";

        RunCommand(Path.GetFullPath("./..", directory), "clone --recurse-submodules", sshConfig, repoUrl);
        RunCommand(directory, "fetch -fu origin", gitRef);
        RunCommand(directory, "checkout --force", StripRef(gitRef));

        _log.Information("Done cloning");
    }

    private void Pull(string repoDirectory, string gitRef)
    {
        _log.Information( "Pulling branch/commit {Ref}...", gitRef);
        _log.Debug("Opening repository in: {RepositoryPath}", repoDirectory);

        //Set an identity
        _log.Debug("Setting identity");
        RunCommand(repoDirectory, "config user.name", $"\"{_configuration.Identity.Name}\"");
        RunCommand(repoDirectory, "config user.email", $"\"{_configuration.Identity.Email}\"");

        if (_configuration.SshCommand != null)
        {
            _log.Debug("Setting ssh command");
            RunCommand(repoDirectory, "config core.sshcommand", $"\"{_configuration.SshCommand}\"");
        }

        _log.Debug("Fetching ref");
        RunCommand(repoDirectory, "fetch -fu origin", gitRef);

        _log.Debug("Checking out {Ref}", StripRef(gitRef));
        RunCommand(repoDirectory, "checkout  --force", StripRef(gitRef));

        _log.Debug("Pulling latest changes");
        RunCommand(repoDirectory, "pull origin HEAD --ff-only --force");

        _log.Debug("Updating submodules");
        RunCommand(repoDirectory, "submodule update --init --recursive");

        _log.Information("Done pulling");
    }

    private string RunCommand(string directory, params string[] arguments)
    {
        var task = _buildService.Run(directory, GitCommand, new List<string>(arguments));
        task.Wait();
        return task.Result;
    }
}
