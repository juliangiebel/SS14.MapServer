using System.Threading.Channels;
using SS14.MapServer.Configuration;

namespace SS14.MapServer.MapProcessing.Services;

public sealed class ProcessQueue
{
    private readonly Channel<ProcessItem> _queue;

    public readonly int MaxItemCount;

    public int Count => _queue.Reader.Count;

    public ProcessQueue(IConfiguration configuration)
    {
        var processConfiguration = new ProcessingConfiguration();
        configuration.Bind(ProcessingConfiguration.Name, processConfiguration);

        MaxItemCount = processConfiguration.ProcessQueueMaxSize;

        var channelConfiguration = new BoundedChannelOptions(MaxItemCount)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        };

        _queue = Channel.CreateBounded<ProcessItem>(channelConfiguration);;
    }

    public async Task<bool> TryQueueProcessItem(ProcessItem item)
    {
        if (Count == MaxItemCount)
            return false;

        await _queue.Writer.WriteAsync(item);
        return true;
    }

    public async ValueTask<ProcessItem> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
