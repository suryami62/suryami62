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

public sealed record SiteProfileSettings(
    string Instagram,
    string Linkedin,
    string Github,
    string Email)
{
    public static SiteProfileSettings Defaults { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);
}

public sealed class SiteProfileSettingsStore(ISettingsRepository repository)
{
    private static readonly string[] Keys =
    [
        SiteProfileSettingKeys.Instagram,
        SiteProfileSettingKeys.Linkedin,
        SiteProfileSettingKeys.Github,
        SiteProfileSettingKeys.Email
    ];

    public async Task<SiteProfileSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var items = await repository.GetValuesAsync(Keys, cancellationToken).ConfigureAwait(false);

        var defaults = SiteProfileSettings.Defaults;

        string Get(string key, string defaultValue)
        {
            return items.TryGetValue(key, out var value) ? value : defaultValue;
        }

        return new SiteProfileSettings(
            Get(SiteProfileSettingKeys.Instagram, defaults.Instagram),
            Get(SiteProfileSettingKeys.Linkedin, defaults.Linkedin),
            Get(SiteProfileSettingKeys.Github, defaults.Github),
            Get(SiteProfileSettingKeys.Email, defaults.Email));
    }

    public async Task SaveAsync(SiteProfileSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [SiteProfileSettingKeys.Instagram] = settings.Instagram,
            [SiteProfileSettingKeys.Linkedin] = settings.Linkedin,
            [SiteProfileSettingKeys.Github] = settings.Github,
            [SiteProfileSettingKeys.Email] = settings.Email
        };

        await repository.UpsertManyAsync(values, cancellationToken).ConfigureAwait(false);
    }
}