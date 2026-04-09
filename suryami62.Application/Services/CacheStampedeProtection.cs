#region

using System.Collections.Concurrent;

#endregion

namespace suryami62.Services;

/// <summary>
///     Provides stampede protection for cache operations to prevent multiple concurrent
///     requests from executing the same expensive operation when cache expires.
/// </summary>
/// <remarks>
///     Stampede protection (a.k.a. thundering herd protection) ensures that when a cache entry
///     expires, only one request fetches the data from the source while others wait for the result.
///     This prevents overwhelming the database/backend with duplicate queries during high traffic.
///     See: https://learn.microsoft.com/aspnet/core/performance/caching/overview?view=aspnetcore-10.0#hybridcache
/// </remarks>
public sealed class CacheStampedeProtection : IDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private bool _disposed;

    /// <summary>
    ///     Disposes all semaphores and clears the dictionary.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        foreach (var semaphore in _locks.Values) semaphore.Dispose();

        _locks.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Gets or creates a semaphore for the specified cache key.
    /// </summary>
    /// <param name="key">The cache key to lock on.</param>
    /// <returns>A semaphore to control concurrent access.</returns>
    public SemaphoreSlim GetLock(string key)
    {
        return _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
    }

    /// <summary>
    ///     Removes the lock for the specified key to prevent memory leaks.
    /// </summary>
    /// <param name="key">The cache key.</param>
    public void ReleaseLock(string key)
    {
        if (_locks.TryRemove(key, out var semaphore)) semaphore.Dispose();
    }

    /// <summary>
    ///     Executes the factory function with stampede protection.
    ///     Only one concurrent execution per key is allowed.
    /// </summary>
    /// <typeparam name="T">The type of the value to return.</typeparam>
    /// <param name="key">The cache key to protect.</param>
    /// <param name="factory">The factory function to execute.</param>
    /// <returns>The result of the factory function.</returns>
    public async Task<T> ExecuteAsync<T>(string key, Func<Task<T>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        var semaphore = GetLock(key);

        await semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            return await factory().ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }
}