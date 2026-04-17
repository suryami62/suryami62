// ============================================================================
// SETTINGS REPOSITORY INTERFACE
// ============================================================================
// This interface defines the contract for application settings storage.
//
// WHAT ARE SETTINGS?
// Settings are key-value pairs stored in the database.
// Unlike appsettings.json (which requires restart to change), these can be
// updated at runtime by administrators.
//
// EXAMPLE USAGE:
// - Site title, description
// - SEO settings (enable sitemap, base URL)
// - Social media links
// - Feature toggles
//
// IReadOnlyDictionary:
// A read-only dictionary means callers cannot modify the returned data.
// They get a copy of the settings, but changes don't affect the repository.
// ============================================================================

namespace suryami62.Application.Persistence;

/// <summary>
///     Defines operations for storing and retrieving application settings.
/// </summary>
public interface ISettingsRepository
{
    /// <summary>
    ///     Gets a single setting value by its key.
    /// </summary>
    /// <param name="key">The setting key (e.g., "Site:Title").</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The setting value, or null if not found.</returns>
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets multiple setting values at once (more efficient than calling GetValueAsync multiple times).
    /// </summary>
    /// <param name="keys">The list of keys to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A read-only dictionary of key-value pairs.</returns>
    Task<IReadOnlyDictionary<string, string>> GetValuesAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates or updates a single setting value.
    ///     If the key exists, it updates. If not, it creates.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates or updates multiple settings at once (batch operation).
    ///     More efficient than calling UpsertAsync multiple times.
    /// </summary>
    /// <param name="values">Dictionary of key-value pairs to save.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    Task UpsertManyAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default);
}