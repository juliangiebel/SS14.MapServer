using Microsoft.EntityFrameworkCore;
using Quartz;
using SS14.MapServer.Models;
using SS14.MapServer.Services;

namespace SS14.MapServer.Jobs;

public class ProcessTiledImage : IJob
{
    public const string ProcessOptionsKey = "Options";

    private readonly ImageProcessingService _processingService;
    private readonly Context _dbContext;

    public ProcessTiledImage(ImageProcessingService processingService, Context dbContext)
    {
        _processingService = processingService;
        _dbContext = dbContext;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var dataMap = context.JobDetail.JobDataMap;

        if (dataMap.Get(ProcessOptionsKey) is not ProcessingOptions options)
            throw new JobExecutionException($"Job data value with key ${ProcessOptionsKey} and type ProcessingOptions is missing");
        
        var tiles = await _processingService.TileImage(
            options.MapId, 
            options.GridId, 
            options.SourcePath, 
            options.TargetPath,
            options.TileSize
            );

        if (options.removeSource)
            File.Delete(options.SourcePath);

        await _dbContext.Tiles!.Where(tile => tile.MapId.Equals(options.MapId) && tile.GridId.Equals(options.GridId))
            .ExecuteDeleteAsync();

        await _dbContext.Tiles!.AddRangeAsync(tiles);
    }

    public record ProcessingOptions(string MapId, int GridId, string SourcePath, string TargetPath, int TileSize, bool removeSource);
}