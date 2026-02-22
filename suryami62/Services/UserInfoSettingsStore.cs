#region

using Microsoft.EntityFrameworkCore;
using suryami62.Data;
using suryami62.Data.Models;

#endregion

namespace suryami62.Services;

internal static class UserInfoSettingKeys
{
    public const string Instagram = "UserInfo:Instagram";
    public const string Linkedin = "UserInfo:Linkedin";
    public const string Github = "UserInfo:Github";
    public const string Email = "UserInfo:Email";
}

internal sealed record UserInfoSettings(
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

internal sealed class UserInfoSettingsStore(ApplicationDbContext context)
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
        var items = await context.Settings
            .AsNoTracking()
            .Where(s => Keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken)
            .ConfigureAwait(false);

        var defaults = UserInfoSettings.Defaults;
        return new UserInfoSettings(
            GetString(items, UserInfoSettingKeys.Instagram, defaults.Instagram),
            GetString(items, UserInfoSettingKeys.Linkedin, defaults.Linkedin),
            GetString(items, UserInfoSettingKeys.Github, defaults.Github),
            GetString(items, UserInfoSettingKeys.Email, defaults.Email));
    }

    public async Task SaveAsync(UserInfoSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var existing = await context.Settings
            .Where(s => Keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, cancellationToken)
            .ConfigureAwait(false);

        Upsert(existing, UserInfoSettingKeys.Instagram, settings.Instagram ?? string.Empty);
        Upsert(existing, UserInfoSettingKeys.Linkedin, settings.Linkedin ?? string.Empty);
        Upsert(existing, UserInfoSettingKeys.Github, settings.Github ?? string.Empty);
        Upsert(existing, UserInfoSettingKeys.Email, settings.Email ?? string.Empty);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private void Upsert(Dictionary<string, Setting> existing, string key, string value)
    {
        if (existing.TryGetValue(key, out var setting))
        {
            setting.Value = value;
            return;
        }

        var created = new Setting { Key = key, Value = value };
        context.Settings.Add(created);
        existing[key] = created;
    }

    private static string GetString(Dictionary<string, string> items, string key, string defaultValue)
    {
        return items.TryGetValue(key, out var value) ? value : defaultValue;
    }
}