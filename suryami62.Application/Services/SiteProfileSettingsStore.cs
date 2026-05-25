#region

using suryami62.Application.Persistence;

#endregion

namespace suryami62.Services;

public static class SiteProfileSettingKeys
{
    public const string Instagram = "UserInfo:Instagram";

    public const string Linkedin = "UserInfo:Linkedin";

    public const string Github = "UserInfo:Github";

    public const string Email = "UserInfo:Email";
}

public sealed record SiteProfileSettings
{
    public string Instagram { get; init; } = string.Empty;

    public string Linkedin { get; init; } = string.Empty;

    public string Github { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public static SiteProfileSettings Defaults { get; } = new();
}

public interface ISiteProfileSettingsStore
{
    Task<SiteProfileSettings> GetAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(SiteProfileSettings settings, CancellationToken cancellationToken = default);
}

public sealed class SiteProfileSettingsStore : ISiteProfileSettingsStore
{
    private const string CacheKey = "siteprofile:settings";

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

    private static readonly string[] Keys = new[]
    {
        SiteProfileSettingKeys.Instagram,
        SiteProfileSettingKeys.Linkedin,
        SiteProfileSettingKeys.Github,
        SiteProfileSettingKeys.Email
    };

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

    public async Task<SiteProfileSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cacheService != null)
        {
            var cached = await _cacheService
                .GetAsync<SiteProfileSettings>(CacheKey, cancellationToken)
                .ConfigureAwait(false);

            if (cached != null)
                return cached;
        }

        if (_stampedeProtection != null && _cacheService != null)
        {
            var result = await _stampedeProtection
                .ExecuteAsync(CacheKey, async () =>
                {
                    var doubleCheck = await _cacheService
                        .GetAsync<SiteProfileSettings>(CacheKey, cancellationToken)
                        .ConfigureAwait(false);

                    if (doubleCheck != null) return doubleCheck;

                    var storedValues = await LoadStoredValuesAsync(cancellationToken)
                        .ConfigureAwait(false);

                    var settings = CreateSettings(storedValues);

                    await _cacheService.SetAsync(CacheKey, settings, CacheExpiration, cancellationToken)
                        .ConfigureAwait(false);

                    return settings;
                }).ConfigureAwait(false);

            return result;
        }

        var fallbackStoredValues = await LoadStoredValuesAsync(cancellationToken)
            .ConfigureAwait(false);

        var fallbackSettings = CreateSettings(fallbackStoredValues);

        if (_cacheService != null)
            await _cacheService.SetAsync(CacheKey, fallbackSettings, CacheExpiration, cancellationToken)
                .ConfigureAwait(false);

        return fallbackSettings;
    }

    public async Task SaveAsync(SiteProfileSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var values = CreatePersistedValues(settings);
        await _repository.UpsertManyAsync(values, cancellationToken).ConfigureAwait(false);

        if (_cacheService != null)
            await _cacheService.RemoveEntryAsync(CacheKey, cancellationToken).ConfigureAwait(false);
    }

    private Task<IReadOnlyDictionary<string, string>> LoadStoredValuesAsync(CancellationToken cancellationToken)
    {
        return _repository.GetValuesAsync(Keys, cancellationToken);
    }

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

    private static string ReadStoredValue(
        IReadOnlyDictionary<string, string> storedValues,
        string key,
        string fallback)
    {
        var found = storedValues.TryGetValue(key, out var value);

        if (found && value != null) return value;

        return fallback;
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