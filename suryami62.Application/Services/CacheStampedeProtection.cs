#region

using System.Collections.Concurrent;

#endregion

namespace suryami62.Services;

public sealed class CacheStampedeProtection : IDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        foreach (var semaphore in _locks.Values) semaphore.Dispose();

        _locks.Clear();

        GC.SuppressFinalize(this);
    }

    public SemaphoreSlim GetLock(string key)
    {
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        return semaphore;
    }

    public void ReleaseLock(string key)
    {
        SemaphoreSlim? semaphore = null;

        try
        {
            _locks.TryRemove(key, out semaphore);
        }
        finally
        {
            semaphore?.Dispose();
        }
    }

    public async Task<T> ExecuteAsync<T>(
        string key,
        Func<Task<T>> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var semaphore = GetLock(key);

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var result = await factory().ConfigureAwait(false);

            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }
}