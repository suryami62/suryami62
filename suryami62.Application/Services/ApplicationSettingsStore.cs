#region

using suryami62.Application.Persistence;

#endregion

namespace suryami62.Services;

public static class ApplicationSettingKeys
{
    public const string RegistrationEnabled = "Registration:Enabled";
}

public sealed record ApplicationSettings(bool RegistrationEnabled)
{
    public static ApplicationSettings Defaults { get; } = new(true);
}

public sealed class ApplicationSettingsStore(ISettingsRepository repository)
{
    public async Task<ApplicationSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var value = await repository
            .GetValueAsync(ApplicationSettingKeys.RegistrationEnabled, cancellationToken)
            .ConfigureAwait(false);

        var registrationEnabled = ParseBoolOrDefault(value, true);

        return new ApplicationSettings(registrationEnabled);
    }

    public async Task SetRegistrationEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        await repository
            .UpsertAsync(ApplicationSettingKeys.RegistrationEnabled, enabled.ToString().ToUpperInvariant(),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static bool ParseBoolOrDefault(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;

        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}