namespace SS14.MapServer.Configuration;

public sealed class GitConfiguration
{
    public const string Name = "Git";

    public string RepositoryUrl { get; set; } = string.Empty;
    public string TargetDirectory { get; set; } = string.Empty;
    public string Branch { get; set; } = "master";
}