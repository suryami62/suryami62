// ============================================================================
// CACHED SETTINGS REPOSITORY
// ============================================================================
// This class adds in-memory caching on top of the database settings repository.
//
// WHAT IS THE DECORATOR PATTERN?
// This class "decorates" (wraps) another ISettingsRepository.
// Think of it like adding a coat of paint - the original object still works,
// but now has extra features (caching) added on top.
//
// WHY TWO LEVELS OF CACHING?
// 1. In-memory cache (this class): Fast, but only for this server instance
// 2. Redis cache (optional): Shared across all servers, survives app restarts
//
// This decorator sits between the application and the database repository:
//   Application -> CachedSettingsRepository -> SettingsRepository -> Database
//
// CACHING STRATEGY:
// - Settings rarely change (site title, SEO settings)
// - But they are read frequently (every page load might read settings)
// - Cache for 5 minutes - balances performance with freshness
//
// CACHE INVALIDATION:
// When settings are updated (Upsert), we remove them from cache immediately.
// This ensures the next read gets the fresh value from database.
//
// BATCH LOOKUPS:
// GetValuesAsync handles multiple keys efficiently:
// - Checks cache for all keys first
// - Only queries database for keys not in cache
// - Stores newly fetched values in cache
// ============================================================================

#region

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using suryami62.Application.Persistence;

#endregion

namespace suryami62.Infrastructure.Persistence;

/// <summary>
///     Adds in-memory caching to the settings repository.
///     Wraps the database repository to reduce database queries.
/// </summary>
public sealed class CachedSettingsRepository : ISettingsRepository
{
    // Cache settings for 5 minutes - settings rarely change
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    // In-memory cache (faster than database, but not shared across servers)
    private readonly IMemoryCache _cache;

    // The underlying database repository (decorated/wrapped object)
    private readonly ISettingsRepository _inner;

    // Logger for cache hit/miss/invalidate diagnostics
    private readonly ILogger<CachedSettingsRepository> _logger;

    /// <summary>
    ///     Creates a cached decorator around a settings repository.
    /// </summary>
    /// <param name="inner">The underlying database repository to wrap.</param>
    /// <param name="cache">The in-memory cache instance.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public CachedSettingsRepository(
        ISettingsRepository inner,
        IMemoryCache cache,
        ILogger<CachedSettingsRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _cache = cache;
        _logger = logger;

        _logger.LogDebug(
            "CachedSettingsRepository initialized with cache duration: {CacheDuration}",
            CacheDuration);
    }

    /// <summary>
    ///     Gets a single setting value with caching.
    ///     Checks cache first, falls back to database if not cached.
    /// </summary>
    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        // Step 1: Build the cache key for this setting
        var cacheKey = BuildCacheKey(key);

        // Step 2: Try to get from in-memory cache first
        if (_cache.TryGetValue(cacheKey, out string? cachedValue))
        {
            // Cache HIT - value found in memory (fast!)
            _logger.LogDebug("Cache HIT for setting '{SettingKey}'", key);
            return cachedValue;
        }

        // Step 3: Cache MISS - need to fetch from database
        _logger.LogDebug("Cache MISS for setting '{SettingKey}'", key);
        var value = await _inner.GetValueAsync(key, cancellationToken).ConfigureAwait(false);

        // Step 4: Store in cache for next time (if value exists)
        if (value != null) SetCache(cacheKey, value);

        return value;
    }

    /// <summary>
    ///     Gets multiple setting values with caching.
    ///     Efficiently handles batch lookups - only queries database for missing keys.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, string>> GetValuesAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        // Step 1: Handle empty key list
        if (keys.Count == 0) return new Dictionary<string, string>(StringComparer.Ordinal);

        // Step 2: Prepare result collection and track missing keys
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var missingKeys = new List<string>(keys.Count);

        // Step 3: Check cache for each key
        foreach (var key in keys)
        {
            var cacheKey = BuildCacheKey(key);

            if (_cache.TryGetValue(cacheKey, out string? cachedValue))
            {
                // Cache HIT for this key
                _logger.LogDebug("Cache HIT for setting '{SettingKey}'", key);

                if (cachedValue != null) result[key] = cachedValue;
            }
            else
            {
                // Cache MISS - need to fetch this key from database
                missingKeys.Add(key);
            }
        }

        // Step 4: If all values were cached, return immediately (fast path)
        if (missingKeys.Count == 0) return result;

        // Step 5: Fetch missing values from database in one batch query
        _logger.LogDebug("Batch cache MISS for {Count} settings", missingKeys.Count);
        var missingValues = await _inner
            .GetValuesAsync(missingKeys, cancellationToken)
            .ConfigureAwait(false);

        // Step 6: Store fetched values in cache and add to result
        foreach (var (key, value) in missingValues)
        {
            var cacheKey = BuildCacheKey(key);
            SetCache(cacheKey, value);
            result[key] = value;
        }

        return result;
    }

    /// <summary>
    ///     Updates a single setting and invalidates its cache entry.
    /// </summary>
    public async Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        // Step 1: Save to database through inner repository
        await _inner.UpsertAsync(key, value, cancellationToken).ConfigureAwait(false);

        // Step 2: Remove from cache so next read gets fresh value
        InvalidateCache(key);
    }

    /// <summary>
    ///     Updates multiple settings and invalidates their cache entries.
    /// </summary>
    public async Task UpsertManyAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        // Step 1: Save all values to database
        await _inner.UpsertManyAsync(values, cancellationToken).ConfigureAwait(false);

        // Step 2: Invalidate cache for all modified keys
        foreach (var key in values.Keys) InvalidateCache(key);
    }

    /// <summary>
    ///     Builds a cache key from the setting key.
    ///     Prefixes with "setting:" to avoid collisions with other cached data.
    /// </summary>
    private static string BuildCacheKey(string settingKey)
    {
        return $"setting:{settingKey}";
    }

    /// <summary>
    ///     Stores a value in the in-memory cache with expiration settings.
    /// </summary>
    private void SetCache(string cacheKey, string value)
    {
        // Configure cache entry options
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration) // Expire after 5 minutes
            .SetPriority(CacheItemPriority.Normal); // Memory pressure handling

        _cache.Set(cacheKey, value, options);
    }

    /// <summary>
    ///     Removes a setting from the cache.
    ///     Called when settings are updated to ensure fresh reads.
    /// </summary>
    private void InvalidateCache(string key)
    {
        var cacheKey = BuildCacheKey(key);
        _cache.Remove(cacheKey);
        _logger.LogDebug("Cache INVALIDATED for setting '{SettingKey}'", key);
    }
}