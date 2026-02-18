#region

using Microsoft.EntityFrameworkCore;
using suryami62.Data;
using suryami62.Data.Models;

#endregion

namespace suryami62.Services;

internal static class SeoFilesSettingKeys
{
    public const string AutoBaseUrl = "SeoFiles:AutoBaseUrl";
    public const string BaseUrl = "SeoFiles:BaseUrl";
    public const string SitemapEnabled = "SeoFiles:SitemapEnabled";
    public const string RobotsEnabled = "SeoFiles:RobotsEnabled";
    public const string DisallowAccount = "SeoFiles:DisallowAccount";
    public const string AdditionalDisallow = "SeoFiles:AdditionalDisallow";
}

internal sealed record SeoFilesSettings(
    bool AutoBaseUrl,
    string BaseUrl,
    bool SitemapEnabled,
    bool RobotsEnabled,
    bool DisallowAccount,
    string AdditionalDisallow)
{
    public static SeoFilesSettings Defaults { get; } = new(
        true,
        string.Empty,
        true,
        true,
        true,
        string.Empty);
}

internal sealed class SeoFilesSettingsStore(ApplicationDbContext context)
{
    private static readonly string[] Keys =
    [
        SeoFilesSettingKeys.AutoBaseUrl,
        SeoFilesSettingKeys.BaseUrl,
        SeoFilesSettingKeys.SitemapEnabled,
        SeoFilesSettingKeys.RobotsEnabled,
        SeoFilesSettingKeys.DisallowAccount,
        SeoFilesSettingKeys.AdditionalDisallow
    ];

    public async Task<SeoFilesSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var items = await context.Settings
            .AsNoTracking()
            .Where(s => Keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken)
            .ConfigureAwait(false);

        var defaults = SeoFilesSettings.Defaults;
        return new SeoFilesSettings(
            GetBool(items, SeoFilesSettingKeys.AutoBaseUrl, defaults.AutoBaseUrl),
            GetString(items, SeoFilesSettingKeys.BaseUrl, defaults.BaseUrl),
            GetBool(items, SeoFilesSettingKeys.SitemapEnabled, defaults.SitemapEnabled),
            GetBool(items, SeoFilesSettingKeys.RobotsEnabled, defaults.RobotsEnabled),
            GetBool(items, SeoFilesSettingKeys.DisallowAccount, defaults.DisallowAccount),
            GetString(items, SeoFilesSettingKeys.AdditionalDisallow, defaults.AdditionalDisallow));
    }

    public async Task SaveAsync(SeoFilesSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var existing = await context.Settings
            .Where(s => Keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, cancellationToken)
            .ConfigureAwait(false);

        Upsert(existing, SeoFilesSettingKeys.AutoBaseUrl, settings.AutoBaseUrl.ToString());
        Upsert(existing, SeoFilesSettingKeys.BaseUrl, settings.BaseUrl ?? string.Empty);
        Upsert(existing, SeoFilesSettingKeys.SitemapEnabled, settings.SitemapEnabled.ToString());
        Upsert(existing, SeoFilesSettingKeys.RobotsEnabled, settings.RobotsEnabled.ToString());
        Upsert(existing, SeoFilesSettingKeys.DisallowAccount, settings.DisallowAccount.ToString());
        Upsert(existing, SeoFilesSettingKeys.AdditionalDisallow, settings.AdditionalDisallow ?? string.Empty);

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

    private static bool GetBool(IReadOnlyDictionary<string, string> items, string key, bool defaultValue)
    {
        if (!items.TryGetValue(key, out var value)) return defaultValue;
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    private static string GetString(IReadOnlyDictionary<string, string> items, string key, string defaultValue)
    {
        return items.TryGetValue(key, out var value) ? value : defaultValue;
    }
}