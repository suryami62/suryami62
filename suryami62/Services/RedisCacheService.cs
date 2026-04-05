#region

using System.Text.Json;
using StackExchange.Redis;

#endregion

namespace suryami62.Services;

/// <summary>
///     Redis-backed distributed cache service implementation.
/// </summary>
internal sealed class RedisCacheService : IRedisCacheService, IDisposable
{
    private readonly IConnectionMultiplexer _connection;
    private readonly IDatabase _database;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

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

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
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
        var serialized = JsonSerializer.Serialize(value, _jsonOptions);
        var expiry = expiration ?? TimeSpan.FromMinutes(30);

        await _database.StringSetAsync(key, serialized, expiry).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(key).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var server = _connection.GetServer(_connection.GetEndPoints().First());
        var keyList = new List<RedisKey>();

        await foreach (var key in server.KeysAsync(pattern: pattern).ConfigureAwait(false)) keyList.Add(key);

        if (keyList.Count > 0) await _database.KeyDeleteAsync(keyList.ToArray()).ConfigureAwait(false);
    }
}