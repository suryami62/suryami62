#region

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

#endregion

namespace suryami62.Services;

internal sealed class RedisCacheService : IRedisCacheService, IDistributedCache
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

    public byte[]? Get(string key)
    {
        var value = _database.StringGet(key);

        if (value.IsNullOrEmpty) return null;

        return (byte[])value!;
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        var value = await _database.StringGetAsync(key).ConfigureAwait(false);

        if (value.IsNullOrEmpty) return null;

        return (byte[])value!;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var expiry = GetExpiration(options);

        if (expiry.HasValue)
            _database.StringSet(key, value, expiry.Value);
        else
            _database.StringSet(key, value);
    }

    public async Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        token.ThrowIfCancellationRequested();

        var expiry = GetExpiration(options);

        if (expiry.HasValue)
            await _database.StringSetAsync(key, value, expiry.Value).ConfigureAwait(false);
        else
            await _database.StringSetAsync(key, value).ConfigureAwait(false);
    }

    public void Refresh(string key)
    {
        _ = _database.KeyExists(key);
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        _ = await _database.KeyExistsAsync(key).ConfigureAwait(false);
    }

    public void Remove(string key)
    {
        _database.KeyDelete(key);
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        await _database.KeyDeleteAsync(key).ConfigureAwait(false);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        var value = await _database.StringGetAsync(key).ConfigureAwait(false);

        if (value.IsNullOrEmpty) return null;

        var stringValue = value.ToString();

        var result = JsonSerializer.Deserialize<T>(stringValue, _jsonOptions);

        return result;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        var serialized = JsonSerializer.Serialize(value, _jsonOptions);

        TimeSpan expiry;
        if (expiration.HasValue)
            expiry = expiration.Value;
        else
            expiry = TimeSpan.FromMinutes(30);

        await _database.StringSetAsync(key, serialized, expiry).ConfigureAwait(false);
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var server = _connection.GetServer(_connection.GetEndPoints()[0]);

        var keyList = new List<RedisKey>();

        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            cancellationToken.ThrowIfCancellationRequested();

            keyList.Add(key);
        }

        if (keyList is { Count: > 0 }) await _database.KeyDeleteAsync(keyList.ToArray()).ConfigureAwait(false);
    }

    public async Task RemoveEntryAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _database.KeyDeleteAsync(key).ConfigureAwait(false);
    }

    private static TimeSpan? GetExpiration(DistributedCacheEntryOptions options)
    {
        if (options.AbsoluteExpiration.HasValue)
        {
            var expirationTime = options.AbsoluteExpiration.Value;
            return expirationTime - DateTimeOffset.UtcNow;
        }

        if (options.AbsoluteExpirationRelativeToNow.HasValue) return options.AbsoluteExpirationRelativeToNow.Value;

        return options.SlidingExpiration;
    }
}