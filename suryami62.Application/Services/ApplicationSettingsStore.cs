#region

using suryami62.Application.Persistence;

#endregion

namespace suryami62.Services;

/// <summary>
///     Declares setting keys used by <see cref="ApplicationSettingsStore" />.
/// </summary>
public static class ApplicationSettingKeys
{
    /// <summary>
    ///     The key that controls whether public account registration is enabled.
    /// </summary>
    public const string RegistrationEnabled = "Registration:Enabled";
}

/// <summary>
///     Represents application-level settings that affect runtime behavior.
/// </summary>
/// <param name="RegistrationEnabled">Indicates whether user registration is enabled.</param>
public sealed record ApplicationSettings(bool RegistrationEnabled)
{
    /// <summary>
    ///     Gets the default application settings used when no persisted values exist.
    /// </summary>
    public static ApplicationSettings Defaults { get; } = new(true);
}

/// <summary>
///     Loads and persists application settings stored in the key/value settings repository.
/// </summary>
/// <remarks>
///     This store backs runtime-wide switches that affect site behavior, such as whether public self-registration
///     remains available from the account area.
/// </remarks>
public sealed class ApplicationSettingsStore(ISettingsRepository repository)
{
    /// <summary>
    ///     Loads the current application settings.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The resolved settings, including fallback defaults.</returns>
    public async Task<ApplicationSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var value = await repository
            .GetValueAsync(ApplicationSettingKeys.RegistrationEnabled, cancellationToken)
            .ConfigureAwait(false);

        var registrationEnabled = ParseBoolOrDefault(value, true);

        return new ApplicationSettings(registrationEnabled);
    }

    /// <summary>
    ///     Persists the registration-enabled flag.
    /// </summary>
    /// <param name="enabled">The value to store.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public async Task SetRegistrationEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        await repository
            .UpsertAsync(ApplicationSettingKeys.RegistrationEnabled, enabled.ToString().ToUpperInvariant(),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Parses a persisted boolean setting while falling back to the supplied default for missing or invalid values.
    /// </summary>
    /// <param name="value">The raw persisted value.</param>
    /// <param name="defaultValue">The fallback value used when parsing cannot succeed.</param>
    /// <returns>The parsed boolean value, or <paramref name="defaultValue" /> when parsing fails.</returns>
    private static bool ParseBoolOrDefault(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;

        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}