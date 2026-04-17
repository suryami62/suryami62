// ============================================================================
// REDIS CACHE SERVICE
// ============================================================================
// This service provides caching using Redis - a fast, in-memory data store.
//
// WHAT IS CACHING?
// Caching stores frequently-accessed data in fast memory so we don't have to
// fetch it from slow sources (like databases) every time.
//
// TWO INTERFACE IMPLEMENTATIONS:
// 1. IRedisCacheService - Custom interface with JSON object caching methods
// 2. IDistributedCache - Standard ASP.NET Core interface for raw bytes
//
// IMPORTANT: The IConnectionMultiplexer (Redis connection) is managed as a
// Singleton by the Dependency Injection container. This service does NOT
// dispose it because it's shared across the entire application lifetime.
//
// See: https://learn.microsoft.com/azure/redis/best-practices-connection
// ============================================================================

#region

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

#endregion

namespace suryami62.Services;

/// <summary>
///     Provides caching using Redis - stores and retrieves data quickly.
///     Implements both custom (IRedisCacheService) and standard (IDistributedCache) caching.
/// </summary>
internal sealed class RedisCacheService : IRedisCacheService, IDistributedCache
{
    // The Redis connection manager (handles multiple Redis servers if needed)
    private readonly IConnectionMultiplexer _connection;

    // The Redis database we read from and write to
    private readonly IDatabase _database;

    // JSON settings for serializing objects to strings
    // CamelCase means "MyProperty" becomes "myProperty" in JSON
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    ///     Creates a new RedisCacheService with an existing Redis connection.
    /// </summary>
    /// <param name="connection">The Redis connection (registered as Singleton in DI).</param>
    public RedisCacheService(IConnectionMultiplexer connection)
    {
        // Ensure connection is not null - throw error if it is
        ArgumentNullException.ThrowIfNull(connection);

        _connection = connection;
        _database = connection.GetDatabase();

        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // myProperty instead of MyProperty
            WriteIndented = false // Compact JSON (no extra whitespace)
        };
    }

    // ============================================================================
    // STANDARD ASP.NET CORE INTERFACE METHODS (IDistributedCache)
    // These methods work with raw byte arrays (lower level)
    // Used by ASP.NET Core's built-in caching features
    // ============================================================================

    /// <summary>
    ///     Gets a cached value as raw bytes (synchronous version).
    ///     Part of the standard IDistributedCache interface.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>Raw bytes, or null if not found.</returns>
    public byte[]? Get(string key)
    {
        // Synchronous version - directly query Redis
        var value = _database.StringGet(key);

        // Check if found
        if (value.IsNullOrEmpty) return null;

        // Convert RedisValue to byte array
        // The ! (null-forgiving) tells the compiler we know it's not null
        return (byte[])value!;
    }

    /// <summary>
    ///     Gets a cached value as raw bytes (asynchronous version).
    ///     Part of the standard IDistributedCache interface.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Raw bytes, or null if not found.</returns>
    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        // Check if operation was cancelled
        token.ThrowIfCancellationRequested();

        // Async fetch from Redis
        var value = await _database.StringGetAsync(key).ConfigureAwait(false);

        if (value.IsNullOrEmpty) return null;

        return (byte[])value!;
    }

    /// <summary>
    ///     Stores raw bytes in the cache (synchronous version).
    ///     Part of the standard IDistributedCache interface.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The raw bytes to store.</param>
    /// <param name="options">Expiration options (when the cache entry should expire).</param>
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        // Step 1: Validate options parameter
        ArgumentNullException.ThrowIfNull(options);

        // Step 2: Calculate expiration from the options (TimeSpan? type)
        var expiry = GetExpiration(options);

        // Step 3: Store in Redis with or without expiration
        // StackExchange.Redis v2.8+ requires non-nullable Expiration.
        // Use different overloads: one with expiration, one without.
        if (expiry.HasValue)
            // Cast TimeSpan to Expiration (implicit conversion operator)
            _database.StringSet(key, value, expiry.Value);
        else
            // No expiration specified - use overload without expiration parameter
            _database.StringSet(key, value);
    }

    /// <summary>
    ///     Stores raw bytes in the cache (asynchronous version).
    ///     Part of the standard IDistributedCache interface.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The raw bytes to store.</param>
    /// <param name="options">Expiration options.</param>
    /// <param name="token">Cancellation token.</param>
    public async Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        // Step 1: Validate options parameter
        ArgumentNullException.ThrowIfNull(options);

        // Step 2: Check if operation was cancelled
        token.ThrowIfCancellationRequested();

        // Step 3: Calculate expiration from options (TimeSpan? type)
        var expiry = GetExpiration(options);

        // Step 4: Store in Redis with or without expiration
        // StackExchange.Redis v2.8+ requires non-nullable Expiration.
        // Use different overloads: one with expiration, one without.
        if (expiry.HasValue)
            // Cast TimeSpan to Expiration (implicit conversion operator)
            await _database.StringSetAsync(key, value, expiry.Value).ConfigureAwait(false);
        else
            // No expiration specified - use overload without expiration parameter
            await _database.StringSetAsync(key, value).ConfigureAwait(false);
    }

    /// <summary>
    ///     "Refreshes" a cache entry - extends its expiration time.
    ///     For Redis with absolute expiration, this is not supported without
    ///     re-setting the value. This method checks if the key exists (no-op).
    /// </summary>
    /// <param name="key">The cache key.</param>
    public void Refresh(string key)
    {
        // Redis with absolute expiration doesn't support sliding/refresh
        // We just check if the key exists (returns boolean, discarded with _)
        _ = _database.KeyExists(key);
    }

    /// <summary>
    ///     Async version of Refresh - checks if key exists.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="token">Cancellation token.</param>
    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        // Check if operation was cancelled
        token.ThrowIfCancellationRequested();

        // Check existence (result discarded with _)
        _ = await _database.KeyExistsAsync(key).ConfigureAwait(false);
    }

    /// <summary>
    ///     Removes a cached item by key (synchronous version).
    /// </summary>
    /// <param name="key">The key to remove.</param>
    public void Remove(string key)
    {
        _database.KeyDelete(key);
    }

    /// <summary>
    ///     Removes a cached item by key (asynchronous version).
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <param name="token">Cancellation token.</param>
    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        // Check if operation was cancelled
        token.ThrowIfCancellationRequested();

        await _database.KeyDeleteAsync(key).ConfigureAwait(false);
    }

    // ============================================================================
    // CUSTOM INTERFACE METHODS (IRedisCacheService)
    // These methods work with typed objects (using JSON serialization)
    // ============================================================================

    /// <summary>
    ///     Gets a cached object by key and deserializes it from JSON.
    /// </summary>
    /// <typeparam name="T">The type of object stored (e.g., BlogPost, List&lt;string&gt;).</typeparam>
    /// <param name="key">The cache key to look up.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The cached object, or null if not found or expired.</returns>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class // "where T : class" means T must be a reference type (not int, bool, etc.)
    {
        // Check if operation was cancelled before starting
        cancellationToken.ThrowIfCancellationRequested();

        // Fetch the value from Redis
        // RedisValue is a special type that can represent strings, bytes, etc.
        var value = await _database.StringGetAsync(key).ConfigureAwait(false);

        // Check if the key doesn't exist or the value is empty
        if (value.IsNullOrEmpty) return null; // Cache miss - not found

        // Convert RedisValue to string
        var stringValue = value.ToString();

        // Deserialize the JSON string back to our object type
        var result = JsonSerializer.Deserialize<T>(stringValue, _jsonOptions);

        return result;
    }

    /// <summary>
    ///     Stores an object in the cache as JSON with an optional expiration time.
    /// </summary>
    /// <typeparam name="T">The type of object to store.</typeparam>
    /// <param name="key">The unique key to identify this cached item.</param>
    /// <param name="value">The object to store.</param>
    /// <param name="expiration">How long to keep this in cache. Default: 30 minutes.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        // Check if operation was cancelled before starting
        cancellationToken.ThrowIfCancellationRequested();

        // Convert the object to a JSON string
        var serialized = JsonSerializer.Serialize(value, _jsonOptions);

        // Use provided expiration, or default to 30 minutes if not specified
        TimeSpan expiry;
        if (expiration.HasValue)
            expiry = expiration.Value;
        else
            expiry = TimeSpan.FromMinutes(30);

        // Store in Redis - StringSetAsync creates or overwrites the value
        await _database.StringSetAsync(key, serialized, expiry).ConfigureAwait(false);
    }

    /// <summary>
    ///     Removes all cached items matching a pattern (e.g., "blog_posts:*").
    ///     Used for cache invalidation when related data changes.
    /// </summary>
    /// <param name="pattern">The Redis key pattern to match (e.g., "user:*").</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Check if operation was cancelled before starting
        cancellationToken.ThrowIfCancellationRequested();

        // Get the Redis server (we use the first endpoint/connection)
        // Redis can have multiple servers (cluster), we use the primary one
        var server = _connection.GetServer(_connection.GetEndPoints()[0]);

        // List to collect all matching keys
        var keyList = new List<RedisKey>();

        // Scan for keys matching the pattern
        // await foreach is IAsyncEnumerable - gets keys one at a time efficiently
        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            // Check cancellation between keys
            cancellationToken.ThrowIfCancellationRequested();

            // Add this key to our list
            keyList.Add(key);
        }

        // Delete all collected keys at once (more efficient than one-by-one)
        if (keyList.Count > 0) await _database.KeyDeleteAsync(keyList.ToArray()).ConfigureAwait(false);
    }

    /// <summary>
    ///     Removes a single cached item by its key.
    /// </summary>
    /// <param name="key">The exact key to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    public async Task RemoveEntryAsync(string key, CancellationToken cancellationToken = default)
    {
        // Check if operation was cancelled before starting
        cancellationToken.ThrowIfCancellationRequested();

        // Delete the key from Redis
        await _database.KeyDeleteAsync(key).ConfigureAwait(false);
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    /// <summary>
    ///     Calculates the expiration time from DistributedCacheEntryOptions.
    ///     Supports three types of expiration:
    ///     1. AbsoluteExpiration - expires at a specific date/time
    ///     2. AbsoluteExpirationRelativeToNow - expires after a duration from now
    ///     3. SlidingExpiration - expires after inactivity period (not directly supported by this calculation)
    /// </summary>
    /// <param name="options">The cache entry options.</param>
    /// <returns>The calculated expiration time span, or null for no expiration.</returns>
    private static TimeSpan? GetExpiration(DistributedCacheEntryOptions options)
    {
        // Check if we have an absolute expiration date
        if (options.AbsoluteExpiration.HasValue)
        {
            // Calculate time remaining until that date
            var expirationTime = options.AbsoluteExpiration.Value;
            return expirationTime - DateTimeOffset.UtcNow;
        }

        // Check if we have a duration from now
        if (options.AbsoluteExpirationRelativeToNow.HasValue) return options.AbsoluteExpirationRelativeToNow.Value;

        // Return sliding expiration (if set) or null
        return options.SlidingExpiration;
    }
}