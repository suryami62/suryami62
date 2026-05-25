#region

using suryami62.Application.Persistence;

#endregion

namespace suryami62.Services;

public static class ApplicationSettingKeys
{
    public const string RegistrationEnabled = "Registration:Enabled";
}

public sealed record ApplicationSettings
{
    public ApplicationSettings(bool registrationEnabled)
    {
        RegistrationEnabled = registrationEnabled;
    }

    public bool RegistrationEnabled { get; init; }

    public static ApplicationSettings Defaults { get; } = new(true);
}

public sealed class ApplicationSettingsStore
{
    private readonly ISettingsRepository _repository;

    public ApplicationSettingsStore(ISettingsRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<ApplicationSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var storedValue = await _repository.GetValueAsync(
            ApplicationSettingKeys.RegistrationEnabled,
            cancellationToken).ConfigureAwait(false);

        var registrationEnabled = ParseBooleanOrDefault(
            storedValue,
            ApplicationSettings.Defaults.RegistrationEnabled);

        return new ApplicationSettings(registrationEnabled);
    }

    public async Task SetRegistrationEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        var value = FormatBooleanForStorage(enabled);

        await _repository.UpsertAsync(
            ApplicationSettingKeys.RegistrationEnabled,
            value,
            cancellationToken).ConfigureAwait(false);
    }

    private static string FormatBooleanForStorage(bool value)
    {
        return value.ToString().ToUpperInvariant();
    }

    private static bool ParseBooleanOrDefault(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;

        if (bool.TryParse(value, out var parsed)) return parsed;

        return defaultValue;
    }
}