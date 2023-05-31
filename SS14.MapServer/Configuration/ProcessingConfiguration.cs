namespace SS14.MapServer.Configuration;

public class ProcessingConfiguration
{
    public const string Name = "Processing";

    /// <summary>
    /// The maximum size of the process directory pool.<br/>
    /// This means that no more than the given amount of directories will be created<br/>
    /// and it in turn dictates the maximum amount of processes that can run in parallel 
    /// </summary>
    public int DirectoryPoolMaxSize { get; set; } = 3;
    
    /// <summary>
    /// This is the target directory for creating the process directory pool 
    /// </summary>
    public string TargetDirectory { get; set; } = string.Empty;

    /// <summary>
    /// The maximum amount of processes that can be queued up before new process requests will be rejected
    /// </summary>
    public int ProcessQueueMaxSize { get; set; } = 6;
}