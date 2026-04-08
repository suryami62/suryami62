#region

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using suryami62.Application.Persistence;

#endregion

namespace suryami62.Infrastructure.Persistence;

/// <summary>
///     Decorator for ISettingsRepository that adds in-memory caching for read operations.
///     Settings rarely change, making them ideal candidates for memory caching.
/// </summary>
public sealed partial class CachedSettingsRepository : ISettingsRepository
{
    // Cache settings for 5 minutes - settings rarely change
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private readonly IMemoryCache _cache;

    private readonly ISettingsRepository _inner;
    private readonly ILogger<CachedSettingsRepository> _logger;

    public CachedSettingsRepository(ISettingsRepository inner, IMemoryCache cache,
        ILogger<CachedSettingsRepository> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        LogRepositoryInitialized(CacheDuration);
    }

    /// <inheritdoc />
    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(key);

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out string? cachedValue))
        {
            LogCacheHit(key);
            return cachedValue;
        }

        // Cache miss - get from underlying repository
        LogCacheMiss(key);
        var value = await _inner.GetValueAsync(key, cancellationToken).ConfigureAwait(false);

        // Store in cache
        if (value != null) SetCache(cacheKey, value);

        return value;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> GetValuesAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        if (keys.Count == 0) return new Dictionary<string, string>(StringComparer.Ordinal);

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var missingKeys = new List<string>(keys.Count);

        // Try to get values from cache
        foreach (var key in keys)
        {
            var cacheKey = BuildCacheKey(key);
            if (_cache.TryGetValue(cacheKey, out string? cachedValue))
            {
                LogCacheHit(key);
                if (cachedValue != null) result[key] = cachedValue;
            }
            else
            {
                missingKeys.Add(key);
            }
        }

        // If all values were cached, return immediately
        if (missingKeys.Count == 0) return result;

        // Fetch missing values from underlying repository
        LogBatchCacheMiss(missingKeys.Count);
        var missingValues = await _inner.GetValuesAsync(missingKeys, cancellationToken).ConfigureAwait(false);

        // Store fetched values in cache and add to result
        foreach (var (key, value) in missingValues)
        {
            var cacheKey = BuildCacheKey(key);
            SetCache(cacheKey, value);
            result[key] = value;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        await _inner.UpsertAsync(key, value, cancellationToken).ConfigureAwait(false);

        // Invalidate cache for this key
        InvalidateCache(key);
    }

    /// <inheritdoc />
    public async Task UpsertManyAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        await _inner.UpsertManyAsync(values, cancellationToken).ConfigureAwait(false);

        // Invalidate cache for all modified keys
        foreach (var key in values.Keys) InvalidateCache(key);
    }

    private static string BuildCacheKey(string settingKey)
    {
        return $"setting:{settingKey}";
    }

    private void SetCache(string cacheKey, string value)
    {
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration)
            .SetPriority(CacheItemPriority.Normal);

        _cache.Set(cacheKey, value, options);
    }

    private void InvalidateCache(string key)
    {
        var cacheKey = BuildCacheKey(key);
        _cache.Remove(cacheKey);
        LogCacheInvalidated(key);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache HIT for setting '{SettingKey}'")]
    private partial void LogCacheHit(string settingKey);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache MISS for setting '{SettingKey}'")]
    private partial void LogCacheMiss(string settingKey);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Batch cache MISS for {Count} settings")]
    private partial void LogBatchCacheMiss(int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache INVALIDATED for setting '{SettingKey}'")]
    private partial void LogCacheInvalidated(string settingKey);

    [LoggerMessage(Level = LogLevel.Debug, Message = "CachedSettingsRepository initialized with cache duration: {CacheDuration}")]
    private partial void LogRepositoryInitialized(TimeSpan cacheDuration);
}