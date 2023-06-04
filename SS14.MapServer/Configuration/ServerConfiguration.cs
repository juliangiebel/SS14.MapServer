using System.Globalization;

namespace SS14.MapServer.Configuration;

public class ServerConfiguration
{
    public const string Name = "Server";

    public Uri Host { get; set; } = new("https://localhost:7154");
    public List<string> CorsOrigins { get; set; } = default!;
    public CultureInfo Language { get; set; } = default!; //new("en-US");
}
