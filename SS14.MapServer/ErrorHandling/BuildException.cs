namespace SS14.MapServer;

public class BuildException : Exception
{
    public BuildException()
    {
    }

    public BuildException(string? message) : base(message)
    {
    }

    public BuildException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}