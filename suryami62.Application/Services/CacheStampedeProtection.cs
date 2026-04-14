// ============================================================================
// CACHE STAMPEDE PROTECTION
// ============================================================================
// This class prevents "thundering herd" problems when cache expires.
//
// WHAT IS A CACHE STAMPEDE?
// When popular cached data expires, many users might request it simultaneously.
// Without protection, ALL would fetch from the database at once - overwhelming it.
//
// EXAMPLE SCENARIO:
// 1. Blog post list is cached for 15 minutes
// 2. Cache expires during high traffic (100 concurrent users)
// 3. WITHOUT protection: All 100 hit the database simultaneously (database crash)
// 4. WITH protection: Only 1 fetches from database, 99 wait and share result
//
// HOW IT WORKS:
// 1. Each cache key has a "semaphore" (like a single-entry door lock)
// 2. First requester acquires the lock and fetches from database
// 3. Other requesters wait for the lock to be released
// 4. When lock released, waiting requests get the cached result
//
// SEMAPHORES:
// A SemaphoreSlim is a threading primitive that allows controlling access.
// - WaitAsync() = wait until it's your turn
// - Release() = let the next person in
// ============================================================================

#region

using System.Collections.Concurrent;

#endregion

namespace suryami62.Services;

/// <summary>
///     Prevents cache stampedes (thundering herd) by ensuring only one request
///     fetches from the database when cache expires. Other requests wait and
///     share the result.
/// </summary>
public sealed class CacheStampedeProtection : IDisposable
{
    // Dictionary that stores a lock (semaphore) for each cache key
    // ConcurrentDictionary is thread-safe for multiple simultaneous operations
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    // Tracks if this object has been disposed (cleanup complete)
    private bool _disposed;

    /// <summary>
    ///     Cleans up all locks when the application shuts down.
    ///     Call this when disposing the service to prevent memory leaks.
    /// </summary>
    public void Dispose()
    {
        // Step 1: Check if already disposed (prevent double-dispose)
        if (_disposed) return;

        // Step 2: Mark as disposed so no new operations start
        _disposed = true;

        // Step 3: Dispose all semaphore objects to release OS resources
        foreach (var semaphore in _locks.Values) semaphore.Dispose();

        // Step 4: Clear the dictionary
        _locks.Clear();

        // Step 5: Tell garbage collector not to call finalizer (optimization)
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Gets or creates a lock (semaphore) for a specific cache key.
    ///     If a lock exists, returns it. If not, creates a new one.
    /// </summary>
    /// <param name="key">The cache key to lock on (e.g., "blogposts:list:true:0:10").</param>
    /// <returns>A semaphore for controlling access to this cache key.</returns>
    public SemaphoreSlim GetLock(string key)
    {
        // Get existing lock or create new one (1 = only 1 allowed at a time)
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        return semaphore;
    }

    /// <summary>
    ///     Removes the lock for a cache key to free memory.
    ///     Call this after cache is populated to clean up unused locks.
    /// </summary>
    /// <param name="key">The cache key whose lock should be removed.</param>
    public void ReleaseLock(string key)
    {
        // Step 1: Declare local variable for the semaphore
        SemaphoreSlim? semaphore = null;

        try
        {
            // Step 2: Try to remove the lock from dictionary
            _locks.TryRemove(key, out semaphore);
        }
        finally
        {
            // Step 3: Always dispose if we got a semaphore
            // This ensures disposal even if an exception occurred
            semaphore?.Dispose();
        }
    }

    /// <summary>
    ///     Executes a function with stampede protection.
    ///     Only one thread can execute the factory function for the same key at a time.
    /// </summary>
    /// <typeparam name="T">The type of data being fetched (e.g., BlogPost).</typeparam>
    /// <param name="key">The cache key to protect (prevents concurrent execution for same key).</param>
    /// <param name="factory">The function to execute (usually fetches from database).</param>
    /// <returns>The result from the factory function (data fetched from database).</returns>
    public async Task<T> ExecuteAsync<T>(string key, Func<Task<T>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        // Step 1: Get the lock (semaphore) for this cache key
        var semaphore = GetLock(key);

        // Step 2: Wait until we can enter (blocks if another thread is inside)
        await semaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            // Step 3: Execute the factory function (fetch from database)
            // Only ONE thread at a time reaches this point per key
            var result = await factory().ConfigureAwait(false);

            // Step 4: Return the result
            return result;
        }
        finally
        {
            // Step 5: ALWAYS release the lock, even if factory threw exception
            // This lets waiting threads proceed
            semaphore.Release();
        }
    }
}