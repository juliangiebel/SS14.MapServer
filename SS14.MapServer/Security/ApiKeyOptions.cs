using Microsoft.AspNetCore.Authentication;

namespace SS14.MapServer.Security;

public class ApiKeyOptions : AuthenticationSchemeOptions
{
    public string? ApiKey { get; set; }
}