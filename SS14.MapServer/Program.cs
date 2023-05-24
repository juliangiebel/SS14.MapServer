using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Quartz;
using SS14.MapServer;
using SS14.MapServer.Models;
using SS14.MapServer.Security;
using SS14.MapServer.Services;
using SS14.MapServer.Services.Interfaces;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Configuration as yaml
builder.Configuration.AddYamlFile("appsettings.yaml", false, true);
builder.Configuration.AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yaml", true, true);
builder.Configuration.AddYamlFile("appsettings.Secret.yaml", true, true);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDbContext<Context>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("default")));
builder.Services.AddScoped<IMapReader, MapReaderService>();
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<ImageProcessingService>();
builder.Services.AddScoped<IJobSchedulingService, JobSchedulingService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.EnableAnnotations();
    c.SwaggerGeneratorOptions.OperationFilters.Add(new MapFormDataParameterFilter());
    c.AddSecurityDefinition(ApiKeyHandler.Name, new OpenApiSecurityScheme
    {
        Description = "API key must appear in header",
        Type = SecuritySchemeType.ApiKey,
        Name = ApiKeyHandler.HeaderName,
        In = ParameterLocation.Header
    });
    
    c.SwaggerGeneratorOptions.OperationFilters.Add(new ExcludeAnonymousSecurityFilter());
});

//Security
builder.Services.AddAuthentication(ApiKeyHandler.Name).AddScheme<ApiKeyOptions, ApiKeyHandler>(
    ApiKeyHandler.Name, 
    options => builder.Configuration.Bind("Auth", options)
    );

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(ApiKeyHandler.Name)
        .RequireAuthenticatedUser()
        .Build();
});

//Scheduler
builder.Services.AddQuartz(q => { q.UseMicrosoftDependencyInjectionJobFactory(); });
builder.Services.AddQuartzServer(q => { q.WaitForJobsToComplete = true; });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();

app.Run();

namespace SS14.MapServer
{
    internal class MapFormDataParameterFilter : IOperationFilter
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
}