using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SS14.MapServer.Security;

public class ExcludeAnonymousSecurityFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var allowsAnonymousAccess = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>().Any();

        if (allowsAnonymousAccess)
            return;
        
        operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
        operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });

        var apiKeyScheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = ApiKeyHandler.Name }
        };

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                [ apiKeyScheme ] = new List<string>
                {
                    "API"
                }
            }
        };
    }
}