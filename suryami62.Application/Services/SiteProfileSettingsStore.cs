// ============================================================================
// SITE PROFILE SETTINGS STORE
// ============================================================================
// This service stores and retrieves the public site profile (social links, email).
//
// WHAT IS THIS FOR?
// Stores contact information and social media links shown on the site:
// - Instagram profile URL
// - LinkedIn profile URL
// - GitHub profile URL
// - Public contact email address
//
// These are editable by admins through a settings page and displayed
// in the footer, contact page, and other areas of the site.
//
// CACHING:
// Profile settings change rarely but are read frequently on every page load.
// We cache for 30 minutes to reduce database queries.
//
// SETTING KEYS:
// - UserInfo:Instagram  - Instagram URL
// - UserInfo:Linkedin   - LinkedIn URL
// - UserInfo:Github     - GitHub URL
// - UserInfo:Email      - Contact email
// ============================================================================

#region

using suryami62.Application.Persistence;

#endregion

namespace suryami62.Services;

/// <summary>
///     Constants for the setting keys used to store profile information.
///     These are the database keys where social links are saved.
/// </summary>
public static class SiteProfileSettingKeys
{
    /// <summary>
    ///     Key for storing Instagram profile URL.
    ///     Full key: "UserInfo:Instagram"
    /// </summary>
    public const string Instagram = "UserInfo:Instagram";

    /// <summary>
    ///     Key for storing LinkedIn profile URL.
    ///     Full key: "UserInfo:Linkedin"
    /// </summary>
    public const string Linkedin = "UserInfo:Linkedin";

    /// <summary>
    ///     Key for storing GitHub profile URL.
    ///     Full key: "UserInfo:Github"
    /// </summary>
    public const string Github = "UserInfo:Github";

    /// <summary>
    ///     Key for storing public contact email.
    ///     Full key: "UserInfo:Email"
    /// </summary>
    public const string Email = "UserInfo:Email";
}

/// <summary>
///     Public profile settings containing social links and contact info.
///     This data is displayed across the site (footer, contact page, etc.).
/// </summary>
public sealed record SiteProfileSettings
{
    /// <summary>
    ///     Instagram profile URL or handle (e.g., "https://instagram.com/myprofile").
    /// </summary>
    public string Instagram { get; init; } = string.Empty;

    /// <summary>
    ///     LinkedIn profile URL (e.g., "https://linkedin.com/in/myprofile").
    /// </summary>
    public string Linkedin { get; init; } = string.Empty;

    /// <summary>
    ///     GitHub profile URL (e.g., "https://github.com/myusername").
    /// </summary>
    public string Github { get; init; } = string.Empty;

    /// <summary>
    ///     Public contact email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    ///     Empty default values when nothing is configured.
    ///     Returns empty strings so the UI can check and hide empty links.
    /// </summary>
    public static SiteProfileSettings Defaults { get; } = new();
}

/// <summary>
///     Interface for site profile settings operations.
///     Allows using fake implementations for testing.
/// </summary>
public interface ISiteProfileSettingsStore
{
    /// <summary>
    ///     Loads the current site profile settings from database (with caching).
    ///     Returns default values if no settings exist.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>Profile settings with social links and email.</returns>
    Task<SiteProfileSettings> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Saves the site profile settings to the database.
    ///     Invalidates the cache so changes appear immediately.
    /// </summary>
    /// <param name="settings">The profile settings to save.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    Task SaveAsync(SiteProfileSettings settings, CancellationToken cancellationToken = default);
}

/// <summary>
///     Loads and saves site profile settings with Redis caching.
///     Uses stampede protection for consistent high-traffic performance.
/// </summary>
public sealed class SiteProfileSettingsStore : ISiteProfileSettingsStore
{
    // Single cache key for all profile settings (stored as one object)
    private const string CacheKey = "siteprofile:settings";

    // Cache entries expire after 30 minutes (profile rarely changes)
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

    // List of all setting keys we need to fetch from database
    // Used for batch loading in GetAsync
    private static readonly string[] Keys = new[]
    {
        SiteProfileSettingKeys.Instagram,
        SiteProfileSettingKeys.Linkedin,
        SiteProfileSettingKeys.Github,
        SiteProfileSettingKeys.Email
    };

    // Optional cache service (null if Redis not configured)
    private readonly IRedisCacheService? _cacheService;

    // Repository for database operations
    private readonly ISettingsRepository _repository;

    // Optional stampede protection (prevents database overload)
    private readonly CacheStampedeProtection? _stampedeProtection;

    /// <summary>
    ///     Creates a new site profile settings store.
    /// </summary>
    /// <param name="repository">The settings database repository.</param>
    /// <param name="cacheService">Optional Redis cache service.</param>
    /// <param name="stampedeProtection">Optional stampede protection locking.</param>
    public SiteProfileSettingsStore(
        ISettingsRepository repository,
        IRedisCacheService? cacheService = null,
        CacheStampedeProtection? stampedeProtection = null)
    {
        _repository = repository;
        _cacheService = cacheService;
        _stampedeProtection = stampedeProtection;
    }

    /// <summary>
    ///     Loads profile settings from database with caching.
    ///     Uses stampede protection to prevent database overload during cache miss.
    /// </summary>
    public async Task<SiteProfileSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        // Step 1: Try to get from cache first
        if (_cacheService != null)
        {
            var cached = await _cacheService
                .GetAsync<SiteProfileSettings>(CacheKey, cancellationToken)
                .ConfigureAwait(false);

            if (cached != null)
                // Cache hit - return immediately
                return cached;
        }

        // Step 2: Cache miss - fetch from database with stampede protection
        if (_stampedeProtection != null && _cacheService != null)
        {
            // Execute with locking - only one thread fetches from database
            var result = await _stampedeProtection
                .ExecuteAsync(CacheKey, async () =>
                {
                    // Step 2a: Double-check cache (another thread might have populated it)
                    var doubleCheck = await _cacheService
                        .GetAsync<SiteProfileSettings>(CacheKey, cancellationToken)
                        .ConfigureAwait(false);

                    if (doubleCheck != null) return doubleCheck;

                    // Step 2b: Batch load all settings from database
                    var storedValues = await LoadStoredValuesAsync(cancellationToken)
                        .ConfigureAwait(false);

                    // Step 2c: Create settings object from raw values
                    var settings = CreateSettings(storedValues);

                    // Step 2d: Store in cache for future requests
                    await _cacheService.SetAsync(CacheKey, settings, CacheExpiration, cancellationToken)
                        .ConfigureAwait(false);

                    return settings;
                }).ConfigureAwait(false);

            return result;
        }

        // Step 3: No stampede protection - fetch directly from database
        var fallbackStoredValues = await LoadStoredValuesAsync(cancellationToken)
            .ConfigureAwait(false);

        var fallbackSettings = CreateSettings(fallbackStoredValues);

        // Store in cache if available
        if (_cacheService != null)
            await _cacheService.SetAsync(CacheKey, fallbackSettings, CacheExpiration, cancellationToken)
                .ConfigureAwait(false);

        return fallbackSettings;
    }

    /// <summary>
    ///     Saves profile settings to the database and clears the cache.
    /// </summary>
    public async Task SaveAsync(SiteProfileSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // Step 1: Save all settings to database (batch upsert)
        var values = CreatePersistedValues(settings);
        await _repository.UpsertManyAsync(values, cancellationToken).ConfigureAwait(false);

        // Step 2: Invalidate cache so changes appear immediately
        if (_cacheService != null)
            await _cacheService.RemoveEntryAsync(CacheKey, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Loads all four profile values from the database in one query.
    ///     More efficient than loading each setting individually.
    /// </summary>
    private Task<IReadOnlyDictionary<string, string>> LoadStoredValuesAsync(CancellationToken cancellationToken)
    {
        return _repository.GetValuesAsync(Keys, cancellationToken);
    }

    /// <summary>
    ///     Creates a SiteProfileSettings object from the raw database values.
    ///     Uses default values (empty strings) for any missing keys.
    /// </summary>
    private static SiteProfileSettings CreateSettings(IReadOnlyDictionary<string, string> storedValues)
    {
        return new SiteProfileSettings
        {
            Instagram = ReadStoredValue(storedValues, SiteProfileSettingKeys.Instagram, string.Empty),
            Linkedin = ReadStoredValue(storedValues, SiteProfileSettingKeys.Linkedin, string.Empty),
            Github = ReadStoredValue(storedValues, SiteProfileSettingKeys.Github, string.Empty),
            Email = ReadStoredValue(storedValues, SiteProfileSettingKeys.Email, string.Empty)
        };
    }

    /// <summary>
    ///     Reads a value from the dictionary, returning fallback if not found.
    ///     Empty string fallback means the UI can check and hide empty links.
    /// </summary>
    private static string ReadStoredValue(
        IReadOnlyDictionary<string, string> storedValues,
        string key,
        string fallback)
    {
        // TryGetValue is safe - returns false if key doesn't exist
        var found = storedValues.TryGetValue(key, out var value);

        // If found but value is null, use fallback; if not found, use fallback
        if (found && value != null) return value;

        return fallback;
    }

    /// <summary>
    ///     Creates a dictionary of all profile values for database storage.
    ///     StringComparer.Ordinal makes key matching case-sensitive and culture-neutral.
    /// </summary>
    private static Dictionary<string, string> CreatePersistedValues(SiteProfileSettings settings)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [SiteProfileSettingKeys.Instagram] = settings.Instagram,
            [SiteProfileSettingKeys.Linkedin] = settings.Linkedin,
            [SiteProfileSettingKeys.Github] = settings.Github,
            [SiteProfileSettingKeys.Email] = settings.Email
        };
    }
}