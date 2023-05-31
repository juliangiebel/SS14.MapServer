using Microsoft.Extensions.ObjectPool;

namespace SS14.MapServer.Helpers;

public sealed class ProcessDirectoryPool
{
    private readonly ObjectPool<ProcessDirectory> _pool;

    /// <summary>
    /// Gets raised when a process directory was returned to the pool
    /// </summary>
    public event EventHandler Returned;

    public int Count { get; private set; } = 0;
    public int MaxPoolSize { get; private set; }
    public int Available { get; private set; } = 0;
    
    public ProcessDirectoryPool(string poolDirectory, int maxPoolSize)
    {
        var directoryInfo = new DirectoryInfo(poolDirectory);
        if (!directoryInfo.Exists)
            throw new DirectoryNotFoundException($"Pool directory {poolDirectory} not found");

        _pool = new DefaultObjectPool<ProcessDirectory>(new ProcessDirectoryPoolPolicy(poolDirectory), maxPoolSize);
        MaxPoolSize = maxPoolSize;
    }

    public bool HasAvailable()
    {
        return Available > 0 || Count < MaxPoolSize;
    }

    public async Task<ProcessDirectory> WaitAvailable(CancellationToken cancellationToken = default)
    {
        var completionSource = new TaskCompletionSource<ProcessDirectory>();

        EventHandler handler = (_, _) =>
        {
            if (HasAvailable())
                completionSource.SetResult(Get());
        };

        Returned += handler;

        try
        {
            static void Cancelled(object? s, CancellationToken cancellationToken) => ((TaskCompletionSource)s!).TrySetCanceled(cancellationToken);

            await using (cancellationToken.UnsafeRegister(Cancelled, completionSource))
            {
                await completionSource.Task;
            }
        }
        finally
        {
            Returned -= handler;
        }

        return await completionSource.Task;
    }

    public void Return(ProcessDirectory directory)
    {
        //Is there some pool implementation that let's you do TryReturn so I don't have to check this twice?
        if (!directory.Directory.Exists)
        {
            Count--;
        }
        else
        {
            Available++;
        }
        
        _pool.Return(directory);
        Returned.Invoke(this, EventArgs.Empty);
    }
    
    private ProcessDirectory Get()
    {
        if (!HasAvailable())
            throw new Exception("Tried getting a process directory while none are available");

        if (Available == 0)
        {
            Count++;
        }
        else
        {
            Available--;
        }

        return _pool.Get();
    }
    
    private class ProcessDirectoryPoolPolicy : IPooledObjectPolicy<ProcessDirectory>
    {
        private readonly DirectoryInfo _poolDirectory;

        public ProcessDirectoryPoolPolicy(string poolDirectory)
        {
            _poolDirectory = new DirectoryInfo(poolDirectory);
        }

        public ProcessDirectory Create()
        {
            var directory = _poolDirectory.CreateSubdirectory(Guid.NewGuid().ToString());
            return new ProcessDirectory(directory);
        }

        public bool Return(ProcessDirectory directory)
        {
            return directory.Directory.Exists;
        }
    }
}