namespace SS14.MapServer.Configuration;

public sealed class BuildConfiguration
{
    public const string Name = "Build";
    
    public bool Enabled { get; set; } = true;
    public BuildRunnerName Runner { get; set; } = BuildRunnerName.Local;
}

public enum BuildRunnerName
{
    Local,
    Container
}