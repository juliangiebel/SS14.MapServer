using Quartz;
using SS14.MapServer.MapProcessing.Services;
using SS14.MapServer.Services;

namespace SS14.MapServer.Jobs;

[DisallowConcurrentExecution]
public class SyncMaps : IJob
{
    public const string MapListKey = "Maps";

    private readonly MapUpdateService _mapUpdateService;

    public SyncMaps(MapUpdateService mapUpdateService)
    {
        _mapUpdateService = mapUpdateService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var dataMap = context.JobDetail.JobDataMap;

        if (dataMap.Get(MapListKey) is not List<string> maps)
            throw new JobExecutionException($"Job data value with key ${MapListKey} and type List<string> is missing");

        //await _mapUpdateService.UpdateMapsFromGit(maps);
    }
}
