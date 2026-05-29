#region

using suryami62.Application.Persistence;

#endregion

namespace suryami62.Services;

public static class SeoSettingKeys
{
    public const string BaseUrl = "Seo:BaseUrl";

    public const string EnableSitemap = "Seo:EnableSitemap";

    public const string EnableRobots = "Seo:EnableRobots";

    public const string RobotsDisallow = "Seo:RobotsDisallow";
}

public sealed record SeoSettings
{
    public const string DefaultRobotsDisallowValue = "/Account";

    public string BaseUrl { get; init; } = string.Empty;

    public bool EnableSitemap { get; init; } = true;

    public bool EnableRobots { get; init; } = true;

    public string RobotsDisallow { get; init; } = DefaultRobotsDisallowValue;

    public static SeoSettings Defaults { get; } = new();
}

public sealed class SeoSettingsStore
{
    private const string EnabledSettingValue = "true";
    private const string DisabledSettingValue = "false";

    private static readonly string[] Keys =
    [
        SeoSettingKeys.BaseUrl,
        SeoSettingKeys.EnableSitemap,
        SeoSettingKeys.EnableRobots,
        SeoSettingKeys.RobotsDisallow
    ];

    private readonly ISettingsRepository _repository;

    public SeoSettingsStore(ISettingsRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<SeoSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var storedValues = await _repository
            .GetValuesAsync(Keys, cancellationToken)
            .ConfigureAwait(false);

        return CreateSettings(storedValues);
    }

    public async Task SaveAsync(SeoSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var values = CreatePersistedValues(settings);
        await _repository.UpsertManyAsync(values, cancellationToken).ConfigureAwait(false);
    }

    private static SeoSettings CreateSettings(IReadOnlyDictionary<string, string> storedValues)
    {
        return new SeoSettings
        {
            BaseUrl = ReadStoredValue(storedValues, SeoSettingKeys.BaseUrl, SeoSettings.Defaults.BaseUrl),
            EnableSitemap = ReadBooleanStoredValue(
                storedValues,
                SeoSettingKeys.EnableSitemap,
                SeoSettings.Defaults.EnableSitemap),
            EnableRobots = ReadBooleanStoredValue(
                storedValues,
                SeoSettingKeys.EnableRobots,
                SeoSettings.Defaults.EnableRobots),
            RobotsDisallow = ReadStoredValue(
                storedValues,
                SeoSettingKeys.RobotsDisallow,
                SeoSettings.Defaults.RobotsDisallow)
        };
    }

    private static string ReadStoredValue(
        IReadOnlyDictionary<string, string> storedValues,
        string key,
        string fallback)
    {
        return storedValues.TryGetValue(key, out var value)
            ? value
            : fallback;
    }

    private static bool ReadBooleanStoredValue(
        IReadOnlyDictionary<string, string> storedValues,
        string key,
        bool fallback)
    {
        if (!storedValues.TryGetValue(key, out var value)) return fallback;

        return !string.Equals(value, DisabledSettingValue, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> CreatePersistedValues(SeoSettings settings)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [SeoSettingKeys.BaseUrl] = settings.BaseUrl ?? string.Empty,
            [SeoSettingKeys.EnableSitemap] = FormatBooleanSettingValue(settings.EnableSitemap),
            [SeoSettingKeys.EnableRobots] = FormatBooleanSettingValue(settings.EnableRobots),
            [SeoSettingKeys.RobotsDisallow] = settings.RobotsDisallow ?? string.Empty
        };
    }

    private static string FormatBooleanSettingValue(bool value)
    {
        return value ? EnabledSettingValue : DisabledSettingValue;
    }
}