namespace SS14.MapServer.Configuration;

public sealed class GitConfiguration
{
    public const string Name = "Git";

    public string RepositoryUrl { get; set; } = string.Empty;
    
    public string TargetDirectory { get; set; } = string.Empty;
    
    public string Branch { get; set; } = "master";
    
    /// <summary>
    /// If true the map server will retrieve the list of changed maps from the github diff api.
    /// If this is false all maps will get updated on every push.
    /// </summary>
    /// <remarks>
    /// Requires the map server to be installed as a github app.
    /// </remarks>
    public bool RetrieveMapFilesFromDiff { get; set; } = true;
    
    /// <summary>
    /// Prevent updating maps when there where any c# files changed.
    /// </summary>
    /// <remarks>
    /// Requires the map server to be installed as a github app.
    /// This setting is recommended when the map server is configured to run for PRs
    /// as it prevents potentially malicious changes from being built and executed.
    /// </remarks>
    public bool DontRunWithCodeChanges { get; set; } = true;

    /// <summary>
    /// Setting this to true enables listening to the PullRequest event for putting the rendered map as a comment into the PR
    /// </summary>
    public bool RunOnPullRequests { get; set; } = true;
}