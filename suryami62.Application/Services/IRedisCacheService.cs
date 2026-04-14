// ============================================================================
// REDIS CACHE SERVICE INTERFACE
// ============================================================================
// This interface defines operations for distributed caching using Redis.
//
// WHAT IS REDIS?
// Redis is a fast in-memory data store used for caching. It runs as a separate
// server and can be shared across multiple web servers.
//
// WHY USE REDIS INSTEAD OF IN-MEMORY CACHE?
// - Survives app restarts (in-memory cache is cleared on restart)
// - Shared across multiple servers (web farm scenarios)
// - Can store more data (not limited to single server memory)
// - Has advanced features (pattern matching, pub/sub, etc.)
//
// CACHE OPERATIONS:
// - GetAsync: Read a value from cache
// - SetAsync: Store a value in cache (with optional expiration)
// - RemoveEntryAsync: Delete a single cached value
// - RemoveByPatternAsync: Delete multiple values matching a pattern (e.g., "blogposts:*")
//
// GENERIC TYPE PARAMETER <T>:
// The 'where T : class' constraint means T must be a reference type (class),
// not a value type (struct). This allows null returns when cache miss occurs.
// ============================================================================

namespace suryami62.Services;

/// <summary>
///     Interface for Redis-backed distributed caching operations.
///     Provides methods to store, retrieve, and remove cached data.
/// </summary>
public interface IRedisCacheService
{
    /// <summary>
    ///     Gets a value from the cache by its key.
    ///     Returns null if the key doesn't exist or has expired.
    /// </summary>
    /// <typeparam name="T">The type of object stored (must be a class).</typeparam>
    /// <param name="key">The cache key to look up (e.g., "blogposts:slug:hello-world").</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    ///     The cached object if found; otherwise null.
    /// </returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    ///     Stores a value in the cache with an optional expiration time.
    ///     If the key already exists, it will be overwritten.
    /// </summary>
    /// <typeparam name="T">The type of object to store (must be a class).</typeparam>
    /// <param name="key">The cache key (e.g., "blogposts:list:true:0:10").</param>
    /// <param name="value">The object to store in cache.</param>
    /// <param name="expiration">How long to keep the data (null = no expiration).</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    ///     Removes a single value from the cache by its key.
    ///     Does nothing if the key doesn't exist.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    Task RemoveEntryAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes multiple values from the cache using a pattern.
    ///     Uses Redis SCAN to find keys matching the pattern, then deletes them.
    /// </summary>
    /// <param name="pattern">
    ///     The pattern to match (e.g., "blogposts:*" matches all blog post keys).
    ///     * = wildcard for any characters.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}