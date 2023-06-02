namespace SS14.MapServer.Configuration;

public class CacheConfiguration
{
    public const string Name = "Cache";

    public double SlidingExpirationMinutes { get; set; } = 720;
    public double RelativeAbsoluteExpirationMinutes { get; set; } = 2880;
}
