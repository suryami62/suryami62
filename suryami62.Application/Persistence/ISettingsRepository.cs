namespace suryami62.Application.Persistence;

/// <summary>
///     Provides persistence operations for application settings stored as key/value pairs.
/// </summary>
public interface ISettingsRepository
{
    /// <summary>
    ///     Gets a setting value by key.
    /// </summary>
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets setting values for a set of keys.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetValuesAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates or updates a setting value.
    /// </summary>
    Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates or updates multiple setting values.
    /// </summary>
    Task UpsertManyAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default);
}