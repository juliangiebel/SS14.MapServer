using Quartz;
using Quartz.Impl;
using SS14.MapServer.Services.Interfaces;

namespace SS14.MapServer.Services;

public class JobSchedulingService : IJobSchedulingService
{
    private readonly ISchedulerFactory _schedulerFactory;

    public JobSchedulingService(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
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

    public async Task ScheduleJob(IJobDetail job, ITrigger trigger)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.ScheduleJob(job, trigger);
    }
}