using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SS14.MapServer.Configuration;
using SS14.MapServer.MapProcessing.Services;
using SS14.MapServer.Models;

namespace SS14.MapServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ManagementController : ControllerBase
{
    private readonly Context _context;
    private readonly ProcessQueue _processQueue;

    private readonly BuildConfiguration _buildConfiguration = new();
    private readonly GitConfiguration _gitConfiguration = new();
    private readonly ProcessingConfiguration _processingConfiguration = new();

    public ManagementController(Context context, IConfiguration configuration, ProcessQueue processQueue)
    {
        _context = context;
        _processQueue = processQueue;
        configuration.Bind(BuildConfiguration.Name, _buildConfiguration);
        configuration.Bind(GitConfiguration.Name, _gitConfiguration);
        configuration.Bind(ProcessingConfiguration.Name, _processingConfiguration);
    }

    [HttpGet("information")]
    [Produces("application/json")]
    public ActionResult<InformationData> GetInformation()
    {
        var information = new InformationData
        {
            Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(),
            Runner = _buildConfiguration.Runner.ToString(),
            AutomatedBuilds = _buildConfiguration.Enabled,
            CleanRendererOutput = _buildConfiguration.CleanMapFolderAfterImport,
            RendererOptions = _buildConfiguration.MapRendererOptionsString,
            DirectoryPoolSize = _processingConfiguration.DirectoryPoolMaxSize,
            ProcessQueueSize = _processingConfiguration.ProcessQueueMaxSize,
            GitConfiguration = _gitConfiguration
        };

        return information;
    }

    [HttpGet("statistics")]
    [Produces("application/json")]
    public async Task<ActionResult<StatisticsData>> GetStatistics()
    {
        var statistics = new StatisticsData
        {
            Maps = await _context.Map!.CountAsync(),
            Grids = await _context.Grid!.CountAsync(),
            Tiles = await _context.Tile!.CountAsync(),
            GeneralImages = await _context.Image!.CountAsync(),
            QueuedWork = _processQueue.Count
        };

        return statistics;
    }
}

public sealed class InformationData
{
    public string? Version { get; set; }
    public string? Runner { get; set; }
    public bool? AutomatedBuilds { get; set; }
    public bool? CleanRendererOutput { get; set; }
    public string? RendererOptions { get; set; }
    public int? DirectoryPoolSize { get; set; }
    public int? ProcessQueueSize { get; set; }
    public GitConfiguration? GitConfiguration { get; set; }
}

public sealed class StatisticsData
{
    public int? Maps { get; set; }
    public int? Grids { get; set; }
    public int? Tiles { get; set; }
    public int? GeneralImages { get; set; }
    public int? QueuedWork { get; set; }
}
