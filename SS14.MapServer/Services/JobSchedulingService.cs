using Quartz;
using Quartz.Impl;
using Serilog;
using SS14.MapServer.Services.Interfaces;
using ILogger = Serilog.ILogger;

namespace SS14.MapServer.Services;

public sealed class JobSchedulingService : IJobSchedulingService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger _log;
    
    public JobSchedulingService(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
        _log = Log.ForContext(typeof(JobSchedulingService));
    }

    public async Task RunJob<T>(string name, string group, JobDataMap? data) where T : IJob
    {
        var job = CreateJob<T>(name, group, data);
        
        var trigger = TriggerBuilder.Create()
            .ForJob(job)
            .WithIdentity($"{name}-trigger", group)
            .StartNow()
            .Build();

        await ScheduleJob(job, trigger);
    }

    public IJobDetail CreateJob<T>(string name, string group, JobDataMap? data) where T : IJob
    {
        return JobBuilder.Create<T>()
            .SetJobData(data)
            .WithIdentity(name, group)
            .Build();
    }

    public async Task ScheduleJob(IJobDetail job, ITrigger trigger, IScheduler? scheduler = null!)
    {
        scheduler ??= await _schedulerFactory.GetScheduler();
        if (await scheduler.CheckExists(job.Key))
        {
            _log.Warning("Tried to schedule a job that's already running: {JobKey}", job.Key);
            return;
        }
        
        await scheduler.ScheduleJob(job, trigger);
    }
}