using System.Collections.Concurrent;
using Serilog;
using SS14.MapServer.Configuration;
using ILogger = Serilog.ILogger;
#pragma warning disable CS4014

namespace SS14.MapServer.MapProcessing.Services;

public sealed class ProcessQueueService : BackgroundService
{
    private readonly ProcessingConfiguration _configuration = new();
    private readonly MapUpdateService _mapUpdateService;
    private readonly ILogger _log;

    private readonly BlockingCollection<ProcessItem> _queue;
    private readonly ProcessDirectoryPool _pool;

    public ProcessQueueService(IConfiguration configuration, MapUpdateService mapUpdateService)
    {
        _mapUpdateService = mapUpdateService;
        configuration.Bind(ProcessingConfiguration.Name, _configuration);
        _log = Log.ForContext<ProcessQueueService>();
        _queue = new BlockingCollection<ProcessItem>(_configuration.ProcessQueueMaxSize);

        _log.Information("Initializing process directory pool", nameof(ProcessQueueService));

        _pool = new ProcessDirectoryPool(_configuration.TargetDirectory, _configuration.DirectoryPoolMaxSize);

        _log.Information(
            "Initialized pool with {Count} of {Max} directories already present",
            _pool.Count,
            _pool.MaxPoolSize
            );
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.Information("{ServiceName} started", nameof(ProcessQueueService));
        return ProcessQueueAsync(stoppingToken);
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        foreach (var processItem in _queue.GetConsumingEnumerable(cancellationToken))
        {
            var directory = await _pool.WaitAvailable(cancellationToken);

            Task.Run(async () =>
            {
                await _mapUpdateService.UpdateMapsFromGit(
                        directory.Directory.FullName,
                        processItem.GitRef,
                        processItem.Maps,
                        cancellationToken)
                    .ContinueWith(
                        task => processItem.OnCompletion.Invoke(task.Result),
                        cancellationToken);

                directory.GitRef = processItem.GitRef;
                _pool.Return(directory);
            }, cancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _log.Information("{ServiceName} stopped", nameof(ProcessQueueService));
        await base.StopAsync(cancellationToken);
    }
}
