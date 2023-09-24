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

    /// <summary>
    /// Enables support for reverse proxy headers like "X-Forwarded-Host" if true. Set this to true if run behind a reverse proxy
    /// </summary>
    public bool UseForwardedHeaders { get; set; } = true;

    /// <summary>
    /// Sets the request base path used before any routes apply i.e. "/base/api/Maps" with "/base" being the PathBase. <br/>
    /// Set this if run behind a reverse proxy on a sub path and the proxy doesn't strip the path the server is hosted on.
    /// </summary>
    /// <remarks>
    /// Add a slash before the path: "/path"
    /// </remarks>
    public string? PathBase { get; set; }

    public int RateLimitCount { get; set; } = 20;
    public long RateLimitWindowMinutes { get; set; } = 1;
    public bool EnableSentry { get; set; } = false;
    /// <summary>
    /// Enables sentries performance monitoring on endpoints if sentry is enabled
    /// </summary>
    public bool EnableSentryTracing { get; set; } = false;
}
