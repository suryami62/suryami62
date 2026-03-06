#region

using suryami62.Application.Persistence;

#endregion

namespace suryami62.Services;

public static class UserInfoSettingKeys
{
    public const string Instagram = "UserInfo:Instagram";
    public const string Linkedin = "UserInfo:Linkedin";
    public const string Github = "UserInfo:Github";
    public const string Email = "UserInfo:Email";
}

public sealed record UserInfoSettings(
    string Instagram,
    string Linkedin,
    string Github,
    string Email)
{
    public static UserInfoSettings Defaults { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);
}

public sealed class UserInfoSettingsStore(ISettingsRepository repository)
{
    private static readonly string[] Keys =
    [
        UserInfoSettingKeys.Instagram,
        UserInfoSettingKeys.Linkedin,
        UserInfoSettingKeys.Github,
        UserInfoSettingKeys.Email
    ];

    public async Task<UserInfoSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var items = await repository.GetValuesAsync(Keys, cancellationToken).ConfigureAwait(false);

        var defaults = UserInfoSettings.Defaults;

        string Get(string key, string defaultValue)
        {
            return items.TryGetValue(key, out var value) ? value : defaultValue;
        }

        return new UserInfoSettings(
            Get(UserInfoSettingKeys.Instagram, defaults.Instagram),
            Get(UserInfoSettingKeys.Linkedin, defaults.Linkedin),
            Get(UserInfoSettingKeys.Github, defaults.Github),
            Get(UserInfoSettingKeys.Email, defaults.Email));
    }

    public async Task SaveAsync(UserInfoSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [UserInfoSettingKeys.Instagram] = settings.Instagram,
            [UserInfoSettingKeys.Linkedin] = settings.Linkedin,
            [UserInfoSettingKeys.Github] = settings.Github,
            [UserInfoSettingKeys.Email] = settings.Email
        };

        await repository.UpsertManyAsync(values, cancellationToken).ConfigureAwait(false);
    }
}