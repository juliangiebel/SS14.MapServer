using System.Collections.Concurrent;
using System.Threading.Channels;
using Serilog;
using SS14.MapServer.Configuration;
using ILogger = Serilog.ILogger;
#pragma warning disable CS4014

namespace SS14.MapServer.MapProcessing.Services;

public sealed class ProcessQueueHostedService : BackgroundService
{
    private readonly ProcessingConfiguration _configuration = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ProcessQueue _queue;
    private readonly ILogger _log;

    private readonly ProcessDirectoryPool _pool;

    public ProcessQueueHostedService(IConfiguration configuration, IServiceProvider serviceProvider, ProcessQueue queue)
    {
        configuration.Bind(ProcessingConfiguration.Name, _configuration);
        _serviceProvider = serviceProvider;
        _queue = queue;
        _log = Log.ForContext<ProcessQueueHostedService>();

        _log.Information("Initializing process directory pool", nameof(ProcessQueueHostedService));

        _pool = new ProcessDirectoryPool(_configuration.TargetDirectory, _configuration.DirectoryPoolMaxSize);

        _log.Information(
            "Initialized pool with {Count} of {Max} directories already present",
            _pool.Count,
            _pool.MaxPoolSize
            );
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.Information("{ServiceName} started", nameof(ProcessQueueHostedService));
        return ProcessQueueAsync(stoppingToken);
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var processItem = await _queue.DequeueAsync(cancellationToken);
            var directory = await _pool.WaitAvailable(cancellationToken);

            Task.Run(async () =>
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var mapUpdateService = scope.ServiceProvider.GetRequiredService<MapUpdateService>();
                        await mapUpdateService.UpdateMapsFromGit(
                                directory.Directory.FullName,
                                processItem.GitRef,
                                processItem.Maps,
                                processItem.RepositoryUrl,
                                cancellationToken)
                            .ContinueWith(
                                task => processItem.OnCompletion.Invoke(task.Result),
                                cancellationToken);
                    }
                    directory.GitRef = processItem.GitRef;
                }
                finally
                {
                    _pool.Return(directory);
                }

            }, cancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _log.Information("{ServiceName} stopped", nameof(ProcessQueueHostedService));
        await base.StopAsync(cancellationToken);
    }
}
