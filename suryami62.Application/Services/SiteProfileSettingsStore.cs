#region

using suryami62.Application.Persistence;

#endregion

namespace suryami62.Services;

/// <summary>
///     Declares setting keys used to store the public site profile.
/// </summary>
public static class SiteProfileSettingKeys
{
    /// <summary>
    ///     The key that stores the Instagram URL or handle.
    /// </summary>
    public const string Instagram = "UserInfo:Instagram";

    /// <summary>
    ///     The key that stores the LinkedIn URL.
    /// </summary>
    public const string Linkedin = "UserInfo:Linkedin";

    /// <summary>
    ///     The key that stores the GitHub URL.
    /// </summary>
    public const string Github = "UserInfo:Github";

    /// <summary>
    ///     The key that stores the public contact email.
    /// </summary>
    public const string Email = "UserInfo:Email";
}

/// <summary>
///     Represents public profile links and contact data shown across the site.
/// </summary>
public sealed record SiteProfileSettings(
    string Instagram,
    string Linkedin,
    string Github,
    string Email)
{
    /// <summary>
    ///     Gets an empty default site profile configuration.
    /// </summary>
    public static SiteProfileSettings Defaults { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);
}

/// <summary>
///     Defines the contract for site profile settings store.
/// </summary>
public interface ISiteProfileSettingsStore
{
    /// <summary>
    ///     Loads the current site profile settings.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The resolved profile settings with fallback defaults.</returns>
    Task<SiteProfileSettings> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Persists the site profile settings.
    /// </summary>
    /// <param name="settings">The settings to store.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task SaveAsync(SiteProfileSettings settings, CancellationToken cancellationToken = default);
}

/// <summary>
///     Loads and persists the site's public profile settings with Redis caching.
/// </summary>
/// <remarks>
///     This store centralizes the public social and contact links rendered across the site's marketing surfaces
///     and edited from the admin profile settings page.
///     Includes stampede protection for consistent high-load performance across all services.
/// </remarks>
public sealed class SiteProfileSettingsStore : ISiteProfileSettingsStore
{
    private const string CacheKey = "siteprofile:settings";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

    private static readonly string[] Keys =
    [
        SiteProfileSettingKeys.Instagram,
        SiteProfileSettingKeys.Linkedin,
        SiteProfileSettingKeys.Github,
        SiteProfileSettingKeys.Email
    ];

    private readonly IRedisCacheService? _cacheService;

    private readonly ISettingsRepository _repository;
    private readonly CacheStampedeProtection? _stampedeProtection;

    public SiteProfileSettingsStore(
        ISettingsRepository repository,
        IRedisCacheService? cacheService = null,
        CacheStampedeProtection? stampedeProtection = null)
    {
        _repository = repository;
        _cacheService = cacheService;
        _stampedeProtection = stampedeProtection;
    }

    /// <inheritdoc />
    public async Task<SiteProfileSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        // Try to get from cache first (cache-aside pattern)
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<SiteProfileSettings>(CacheKey, cancellationToken)
                .ConfigureAwait(false);
            if (cached != null) return cached;
        }

        // Cache miss - load from repository with stampede protection
        // This prevents multiple concurrent requests from executing the same expensive operation
        if (_stampedeProtection != null && _cacheService != null)
            return await _stampedeProtection.ExecuteAsync(CacheKey, async () =>
            {
                // Double-check cache after acquiring lock (another thread might have populated it)
                var doubleCheck = await _cacheService.GetAsync<SiteProfileSettings>(CacheKey, cancellationToken)
                    .ConfigureAwait(false);
                if (doubleCheck != null) return doubleCheck;

                // Fetch from repository
                var storedValues = await LoadStoredValuesAsync(cancellationToken).ConfigureAwait(false);
                var settings = CreateSettings(storedValues);

                // Store in cache
                await _cacheService.SetAsync(CacheKey, settings, CacheExpiration, cancellationToken)
                    .ConfigureAwait(false);

                return settings;
            }).ConfigureAwait(false);

        // Fallback without stampede protection (when not configured)
        var fallbackStoredValues = await LoadStoredValuesAsync(cancellationToken).ConfigureAwait(false);
        var fallbackSettings = CreateSettings(fallbackStoredValues);

        if (_cacheService != null)
            await _cacheService.SetAsync(CacheKey, fallbackSettings, CacheExpiration, cancellationToken)
                .ConfigureAwait(false);

        return fallbackSettings;
    }

    /// <inheritdoc />
    public async Task SaveAsync(SiteProfileSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _repository.UpsertManyAsync(CreatePersistedValues(settings), cancellationToken).ConfigureAwait(false);

        // Invalidate cache
        if (_cacheService != null)
            await _cacheService.RemoveEntryAsync(CacheKey, cancellationToken).ConfigureAwait(false);
    }

    private Task<IReadOnlyDictionary<string, string>> LoadStoredValuesAsync(CancellationToken cancellationToken)
    {
        return _repository.GetValuesAsync(Keys, cancellationToken);
    }

    private static SiteProfileSettings CreateSettings(IReadOnlyDictionary<string, string> storedValues)
    {
        return new SiteProfileSettings(
            ReadStoredValue(storedValues, SiteProfileSettingKeys.Instagram, SiteProfileSettings.Defaults.Instagram),
            ReadStoredValue(storedValues, SiteProfileSettingKeys.Linkedin, SiteProfileSettings.Defaults.Linkedin),
            ReadStoredValue(storedValues, SiteProfileSettingKeys.Github, SiteProfileSettings.Defaults.Github),
            ReadStoredValue(storedValues, SiteProfileSettingKeys.Email, SiteProfileSettings.Defaults.Email));
    }

    private static string ReadStoredValue(
        IReadOnlyDictionary<string, string> storedValues,
        string key,
        string fallback)
    {
        // Empty-string fallbacks keep the public profile safe to render even while setup is only partially complete.
        return storedValues.TryGetValue(key, out var value)
            ? value ?? fallback
            : fallback;
    }

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