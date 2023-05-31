namespace SS14.MapServer.MapProcessing;

/// <summary>
/// Points to a directory that is used for processing map render requests
/// </summary>
public sealed class ProcessDirectory : IEquatable<ProcessDirectory>
{
    /// <summary>
    /// The commit the repository inside this processing directory is on
    /// </summary>
    public string? GitRef;
    
    /// <summary>
    /// The rooted path to this directory
    /// </summary>
    public readonly DirectoryInfo Directory;

    public ProcessDirectory(DirectoryInfo directory)
    {
        Directory = directory;
    }

    public bool Equals(ProcessDirectory? other)
    {
        if (ReferenceEquals(null, other)) return false;
        return ReferenceEquals(this, other) || Directory.Equals(other.Directory);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ProcessDirectory other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Directory.GetHashCode();
    }
}