#region

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

#endregion

namespace suryami62.Services;

/// <summary>
///     Redis-backed distributed cache service implementation.
///     Implements both IRedisCacheService (for advanced Redis operations) and IDistributedCache (for ASP.NET Core
///     compatibility).
/// </summary>
/// <remarks>
///     IMPORTANT: IConnectionMultiplexer is registered as Singleton and managed by DI container.
///     This service does NOT dispose the ConnectionMultiplexer as it's shared across the application lifetime.
///     See: https://learn.microsoft.com/azure/redis/best-practices-connection#using-forcereconnect-with-stackexchangeredis
/// </remarks>
public sealed class RedisCacheService : IRedisCacheService, IDistributedCache
{
    private readonly IConnectionMultiplexer _connection;
    private readonly IDatabase _database;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IConnectionMultiplexer connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _database = connection.GetDatabase();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    #region IRedisCacheService Implementation

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await _database.StringGetAsync(key).ConfigureAwait(false);

        if (value.IsNullOrEmpty) return null;

        var stringValue = value.ToString();
        return JsonSerializer.Deserialize<T>(stringValue, _jsonOptions);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        var serialized = JsonSerializer.Serialize(value, _jsonOptions);
        var expiry = expiration ?? TimeSpan.FromMinutes(30);

        await _database.StringSetAsync(key, serialized, expiry).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var server = _connection.GetServer(_connection.GetEndPoints()[0]);
        var keyList = new List<RedisKey>();

        await foreach (var key in server.KeysAsync(pattern: pattern).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            keyList.Add(key);
        }

        if (keyList.Count > 0)
            await _database.KeyDeleteAsync(keyList.ToArray()).ConfigureAwait(false);
    }

    /// <inheritdoc cref="IRedisCacheService.RemoveEntryAsync" />
    public async Task RemoveEntryAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _database.KeyDeleteAsync(key).ConfigureAwait(false);
    }

    #endregion

    #region IDistributedCache Implementation

    /// <inheritdoc />
    public byte[]? Get(string key)
    {
        var value = _database.StringGet(key);
        return value.IsNullOrEmpty ? null : (byte[])value!;
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var value = await _database.StringGetAsync(key).ConfigureAwait(false);
        return value.IsNullOrEmpty ? null : (byte[])value!;
    }

    /// <inheritdoc />
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var expiry = GetExpiration(options);
        _database.StringSet(key, value, expiry);
    }

    /// <inheritdoc />
    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        token.ThrowIfCancellationRequested();
        var expiry = GetExpiration(options);
        await _database.StringSetAsync(key, value, expiry).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Refresh(string key)
    {
        // Redis TTL-based expiry doesn't support refresh without re-setting
        // This is a no-op for absolute expiration
        _ = _database.KeyExists(key);
    }

    /// <inheritdoc />
    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        _ = await _database.KeyExistsAsync(key).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        _database.KeyDelete(key);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        await _database.KeyDeleteAsync(key).ConfigureAwait(false);
    }

    private static TimeSpan? GetExpiration(DistributedCacheEntryOptions options)
    {
        if (options.AbsoluteExpiration.HasValue)
            return options.AbsoluteExpiration.Value - DateTimeOffset.UtcNow;

        if (options.AbsoluteExpirationRelativeToNow.HasValue)
            return options.AbsoluteExpirationRelativeToNow.Value;

        return options.SlidingExpiration;
    }

    #endregion
}