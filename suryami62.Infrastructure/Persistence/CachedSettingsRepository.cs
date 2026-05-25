#region

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using suryami62.Application.Persistence;

#endregion

namespace suryami62.Infrastructure.Persistence;

public sealed class CachedSettingsRepository : ISettingsRepository
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;

    private readonly ISettingsRepository _inner;

    private readonly ILogger<CachedSettingsRepository> _logger;

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

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(key);

        if (_cache.TryGetValue(cacheKey, out string? cachedValue))
        {
            _logger.LogDebug("Cache HIT for setting '{SettingKey}'", key);
            return cachedValue;
        }

        _logger.LogDebug("Cache MISS for setting '{SettingKey}'", key);
        var value = await _inner.GetValueAsync(key, cancellationToken).ConfigureAwait(false);

        if (value != null) SetCache(cacheKey, value);

        return value;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetValuesAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        if (keys.Count == 0) return new Dictionary<string, string>(StringComparer.Ordinal);

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var missingKeys = new List<string>(keys.Count);

        foreach (var key in keys)
        {
            var cacheKey = BuildCacheKey(key);

            if (_cache.TryGetValue(cacheKey, out string? cachedValue))
            {
                _logger.LogDebug("Cache HIT for setting '{SettingKey}'", key);

                if (cachedValue != null) result[key] = cachedValue;
            }
            else
            {
                missingKeys.Add(key);
            }
        }

        if (missingKeys.Count == 0) return result;

        _logger.LogDebug("Batch cache MISS for {Count} settings", missingKeys.Count);
        var missingValues = await _inner
            .GetValuesAsync(missingKeys, cancellationToken)
            .ConfigureAwait(false);

        foreach (var (key, value) in missingValues)
        {
            var cacheKey = BuildCacheKey(key);
            SetCache(cacheKey, value);
            result[key] = value;
        }

        return result;
    }

    public async Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        await _inner.UpsertAsync(key, value, cancellationToken).ConfigureAwait(false);

        InvalidateCache(key);
    }

    public async Task UpsertManyAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        await _inner.UpsertManyAsync(values, cancellationToken).ConfigureAwait(false);

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
        _logger.LogDebug("Cache INVALIDATED for setting '{SettingKey}'", key);
    }
}