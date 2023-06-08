using System.Globalization;

namespace SS14.MapServer.Configuration;

public class ServerConfiguration
{
    public const string Name = "Server";

    public Uri Host { get; set; } = new("https://localhost:7154");
    public List<string> CorsOrigins { get; set; } = default!;
    public CultureInfo Language { get; set; } = default!; //new("en-US");

    /// <summary>
    /// Enables https redirection if true. Set this to false if run behind a reverse proxy
    /// </summary>
    public bool UseHttps { get; set; } = false;

    public int RateLimitCount { get; set; } = 20;
    public long RateLimitWindowMinutes { get; set; } = 1;
}
