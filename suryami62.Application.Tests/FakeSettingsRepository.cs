#region

using suryami62.Application.Persistence;

#endregion

namespace suryami62.Application.Tests;

/// <summary>
///     In-memory implementation of <see cref="ISettingsRepository" /> for use in unit tests.
/// </summary>
internal sealed class FakeSettingsRepository : ISettingsRepository
{
    private readonly Dictionary<string, string> _store = new(StringComparer.Ordinal);

    public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(key, out var value);
        return Task.FromResult<string?>(value);
    }

    public Task<IReadOnlyDictionary<string, string>> GetValuesAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, string> result = keys
            .Where(_store.ContainsKey)
            .ToDictionary(k => k, k => _store[k], StringComparer.Ordinal);

        return Task.FromResult(result);
    }

    public Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        _store[key] = value;
        return Task.CompletedTask;
    }

    public Task UpsertManyAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        foreach (var (k, v) in values) _store[k] = v;

        return Task.CompletedTask;
    }
}