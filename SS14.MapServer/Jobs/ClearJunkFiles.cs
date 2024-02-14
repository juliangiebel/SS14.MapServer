using Quartz;
using SS14.MapServer.Services;

namespace SS14.MapServer.Jobs;

// Fires daily at 2 am
[CronSchedule("0 0 2 * * ?", "ClearJunkFiles", "maintenance")]
public class ClearJunkFiles : IJob
{
    private readonly FileManagementService _fileManagementService;

    public ClearJunkFiles(FileManagementService fileManagementService)
    {
        _fileManagementService = fileManagementService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _fileManagementService.CleanBuildDirectories();
    }
}
