using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Quartz;
using Serilog;
using SS14.GithubApiHelper.Extensions;
using SS14.GithubApiHelper.Services;
using SS14.MapServer;
using SS14.MapServer.BuildRunners;
using SS14.MapServer.Configuration;
using SS14.MapServer.Helpers;
using SS14.MapServer.MapProcessing.Services;
using SS14.MapServer.Models;
using SS14.MapServer.Security;
using SS14.MapServer.Services;
using SS14.MapServer.Services.Github;
using SS14.MapServer.Services.Interfaces;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Configuration as yaml
builder.Configuration.AddYamlFile("appsettings.yaml", false, true);
builder.Configuration.AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yaml", true, true);
builder.Configuration.AddYamlFile("appsettings.Secret.yaml", true, true);

// Controllers and Caching
builder.Services.AddResponseCaching();
builder.Services.AddControllers(options =>
{
    options.CacheProfiles.Add("Default", new CacheProfile()
    {
        Duration = 60
    });
});

//Cors
var corsConfiguration = new ServerConfiguration();
builder.Configuration.Bind(ServerConfiguration.Name, corsConfiguration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsConfiguration.CorsOrigins.ToArray());
        policy.AllowCredentials();
    });
});

//DB
builder.Services.AddDbContext<Context>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("default")));

//Services
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<ImageProcessingService>();
builder.Services.AddScoped<IJobSchedulingService, JobSchedulingService>();
builder.Services.AddScoped<IMapReaderService, MapReaderServiceService>();
builder.Services.AddScoped<MapUpdateService>();

builder.Services.AddSingleton<GithubApiService>();
builder.Services.AddSingleton<RateLimiterService>();
builder.Services.AddSingleton<ContainerService>();
builder.Services.AddSingleton<LocalBuildService>();
builder.Services.AddSingleton<GitService>();
builder.Services.AddSingleton<StartupCheckService>();
builder.Services.AddSingleton<ProcessQueue>();

builder.Services.AddHostedService<ProcessQueueHostedService>();

//Github
builder.Services.AddGithubTemplating();

//Swagger
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

//Logging
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));
builder.Logging.AddSerilog();

var app = builder.Build();

Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);

//Preflight Checks
Log.Information("Running preflight checks...");
var checkResult = await app.Services.GetService<StartupCheckService>()?.RunStartupCheck()!;
if (!checkResult)
{
    Log.Fatal("Some preflight checks didn't pass. Shutting down...");
    await app.DisposeAsync();
    return -1;
}
Log.Information("Preflight checks passed");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsProduction() || app.Environment.IsStaging())
{
    //app.UseHttpsRedirection();
}

app.UseCors();
app.UseResponseCaching();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();

await app.PreloadGithubTemplates();
/*var test = app.Services.GetService<GithubTemplateService>();
var result = await test?.RenderTemplate("generating_map", new
{
    test = "test",
    changedFiles = new[]
    {
        "Resources/Maps/testA.yml",
        "Resources/Maps/testB.yml",
    },
    newFiles = new[]
    {
        "Resources/Maps/testC.yml"
    },
})!;*/

app.Run();
return 0;
