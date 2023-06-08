using System.Threading.RateLimiting;
using SS14.MapServer.Configuration;

namespace SS14.MapServer.Extensions;

public static class ApiRateLimitingExtension
{
    public static void AddApiRateLimiting(this IServiceCollection services, ServerConfiguration configuration)
    {
        var globalRateLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Request.Host.ToString(),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = configuration.RateLimitCount,
                    Window = TimeSpan.FromMinutes(configuration.RateLimitWindowMinutes)
                }
            )
        );

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = globalRateLimiter;
        });
    }
}
