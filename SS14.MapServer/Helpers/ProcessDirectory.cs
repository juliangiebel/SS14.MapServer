namespace SS14.MapServer.Helpers;

/// <summary>
/// Points to a directory that is used for processing map render requests
/// </summary>
public sealed class ProcessDirectory
{
    /// <summary>
    /// The commit the repository inside this processing directory is on
    /// </summary>
    public string? CommitHash;
    
    /// <summary>
    /// The rooted path to this directory
    /// </summary>
    public readonly DirectoryInfo Directory;

    public ProcessDirectory(DirectoryInfo directory)
    {
        Directory = directory;
    }
}