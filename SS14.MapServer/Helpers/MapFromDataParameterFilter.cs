using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SS14.MapServer.Helpers;

public class MapFormDataParameterFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var methodName = context.MethodInfo.Name;

        if (methodName != "PostMap" && methodName != "PutMap")
            return;

        if(!operation.RequestBody.Content.TryGetValue("multipart/form-data", out var type))
            return;

        if(!type.Schema.Properties.TryGetValue("images", out var imagesParameter))
            return;

        if(!type.Encoding.TryGetValue("images", out var imageEncoding))
            return;

        var mapEncoding = new OpenApiEncoding
        {
            Style = ParameterStyle.Form
        };

        type.Encoding.Clear();
        type.Encoding.Add("image", imageEncoding);
        type.Encoding.Add("map", mapEncoding);

        var mapParameter = new OpenApiSchema
        {
            Type = "string"
        };

        type.Schema.Properties.Clear();
        type.Schema.Properties.Add("images", imagesParameter);
        type.Schema.Properties.Add("map", mapParameter);

        type.Schema.Required.Clear();
    }
}
