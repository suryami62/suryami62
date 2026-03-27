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
///     Loads and persists the site's public profile settings.
/// </summary>
/// <remarks>
///     This store centralizes the public social and contact links rendered across the site's marketing surfaces
///     and edited from the admin profile settings page.
/// </remarks>
public sealed class SiteProfileSettingsStore(ISettingsRepository repository)
{
    private static readonly string[] Keys =
    [
        SiteProfileSettingKeys.Instagram,
        SiteProfileSettingKeys.Linkedin,
        SiteProfileSettingKeys.Github,
        SiteProfileSettingKeys.Email
    ];

    /// <summary>
    ///     Loads the current site profile settings.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The resolved profile settings with fallback defaults.</returns>
    public async Task<SiteProfileSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var storedValues = await LoadStoredValuesAsync(cancellationToken).ConfigureAwait(false);
        return CreateSettings(storedValues);
    }

    /// <summary>
    ///     Persists the site profile settings.
    /// </summary>
    /// <param name="settings">The settings to store.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public async Task SaveAsync(SiteProfileSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await repository.UpsertManyAsync(CreatePersistedValues(settings), cancellationToken).ConfigureAwait(false);
    }

    private Task<IReadOnlyDictionary<string, string>> LoadStoredValuesAsync(CancellationToken cancellationToken)
    {
        return repository.GetValuesAsync(Keys, cancellationToken);
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