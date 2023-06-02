using Microsoft.EntityFrameworkCore;
using Quartz;
using SS14.MapServer.MapProcessing.Services;
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
            options.MapGuid,
            options.GridId,
            options.SourcePath,
            options.TargetPath,
            options.TileSize
            );

        if (options.removeSource)
        {
            try
            {
                File.Delete(options.SourcePath);
            }
            catch
            {
                // ignored
            }
        }

        await _dbContext.Tile!.Where(tile => tile.MapGuid.Equals(options.MapGuid) && tile.GridId.Equals(options.GridId))
            .ExecuteDeleteAsync();

        _dbContext.Tile!.AddRange(tiles);
        await _dbContext.SaveChangesAsync();
    }

    public record ProcessingOptions(Guid MapGuid, int GridId, string SourcePath, string TargetPath, int TileSize, bool removeSource);
}
