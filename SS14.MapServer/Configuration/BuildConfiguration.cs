namespace SS14.MapServer.Configuration;

public sealed class BuildConfiguration
{
    public const string Name = "Build";
    
    public bool Enabled { get; set; } = true;
    public BuildRunnerName Runner { get; set; } = BuildRunnerName.Local;
    public string RelativeOutputPath { get; set; } = "bin";
    public string MapRendererProjectName { get; set; } = "Content.MapRenderer";
    public string MapRendererCommand { get; set; } = "Content.MapRenderer.exe";
    public string MapRendererOptionsString { get; set; } = "--format webp --viewer -f";
    public int ProcessTimeoutMinutes { get; set; } = 10;
}

public enum BuildRunnerName
{
    Local,
    Container
}