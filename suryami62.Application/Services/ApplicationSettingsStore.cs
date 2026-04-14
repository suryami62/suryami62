// ============================================================================
// APPLICATION SETTINGS STORE
// ============================================================================
// This file manages runtime settings stored in the database (not appsettings.json).
//
// WHAT IS THIS FOR?
// Settings here can be changed at runtime by administrators without restarting
// the application. For example: enabling/disabling user registration.
//
// WHY NOT JUST USE appsettings.json?
// - appsettings.json requires app restart to change
// - These settings can be changed via admin UI immediately
// - Multiple servers share the same database settings
//
// CURRENT SETTINGS:
// - RegistrationEnabled: Whether new users can register (true/false)
//
// USAGE:
// Inject ApplicationSettingsStore where needed:
//   public class MyService(ApplicationSettingsStore settings) { ... }
//
// Get settings:
//   ApplicationSettings settings = await settingsStore.GetAsync();
//   if (settings.RegistrationEnabled) { ... }
// ============================================================================

#region

using suryami62.Application.Persistence;

#endregion

namespace suryami62.Services;

/// <summary>
///     Constants for setting keys stored in the database.
///     Using constants prevents typos when reading/writing settings.
/// </summary>
public static class ApplicationSettingKeys
{
    /// <summary>
    ///     Key for the registration enabled setting.
    ///     Full path in DB: "Registration:Enabled"
    /// </summary>
    public const string RegistrationEnabled = "Registration:Enabled";
}

/// <summary>
///     Application settings that can be changed at runtime.
///     This is a simple data container (record) for settings values.
/// </summary>
public sealed record ApplicationSettings
{
    /// <summary>
    ///     Creates a new settings instance with specified values.
    /// </summary>
    /// <param name="registrationEnabled">Whether registration is enabled.</param>
    public ApplicationSettings(bool registrationEnabled)
    {
        RegistrationEnabled = registrationEnabled;
    }

    /// <summary>
    ///     Whether new user registration is enabled.
    ///     When false, the register page shows "registration is disabled".
    /// </summary>
    public bool RegistrationEnabled { get; init; }

    /// <summary>
    ///     Default settings when nothing is stored in database.
    ///     Registration is enabled by default (true).
    /// </summary>
    public static ApplicationSettings Defaults { get; } = new(true);
}

/// <summary>
///     Loads and saves application settings to the database.
///     Uses ISettingsRepository for actual database operations.
/// </summary>
public sealed class ApplicationSettingsStore
{
    // Repository for reading/writing settings to database
    private readonly ISettingsRepository _repository;

    /// <summary>
    ///     Creates a new settings store with the given repository.
    /// </summary>
    /// <param name="repository">The settings database repository.</param>
    public ApplicationSettingsStore(ISettingsRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <summary>
    ///     Loads the current application settings from the database.
    ///     If a setting is missing, uses the default value.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The current application settings.</returns>
    public async Task<ApplicationSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        // Step 1: Read the raw string value from database
        var storedValue = await _repository.GetValueAsync(
            ApplicationSettingKeys.RegistrationEnabled,
            cancellationToken).ConfigureAwait(false);

        // Step 2: Convert the string to a boolean (with default fallback)
        var registrationEnabled = ParseBooleanOrDefault(
            storedValue,
            ApplicationSettings.Defaults.RegistrationEnabled);

        // Step 3: Create and return the settings object
        return new ApplicationSettings(registrationEnabled);
    }

    /// <summary>
    ///     Saves the registration enabled setting to the database.
    /// </summary>
    /// <param name="enabled">Whether to enable registration (true) or disable (false).</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    public async Task SetRegistrationEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        // Step 1: Convert boolean to string format
        var value = FormatBooleanForStorage(enabled);

        // Step 2: Save to database (creates if new, updates if exists)
        await _repository.UpsertAsync(
            ApplicationSettingKeys.RegistrationEnabled,
            value,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Converts a boolean to string format for database storage.
    ///     Uses uppercase "True"/"False" for consistency.
    /// </summary>
    private static string FormatBooleanForStorage(bool value)
    {
        // Example: true → "TRUE", false → "FALSE"
        // Uppercase is consistent and easy to read in database
        return value.ToString().ToUpperInvariant();
    }

    /// <summary>
    ///     Parses a boolean string from the database.
    ///     Returns default value if string is null, empty, or invalid.
    /// </summary>
    /// <param name="value">The raw string value from database (e.g., "TRUE", "FALSE").</param>
    /// <param name="defaultValue">Value to return if parsing fails.</param>
    /// <returns>The parsed boolean, or defaultValue if invalid.</returns>
    private static bool ParseBooleanOrDefault(string? value, bool defaultValue)
    {
        // Step 1: Handle null or empty string
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;

        // Step 2: Try to parse the string as boolean
        // This handles "True", "true", "TRUE", "False", "false", "FALSE"
        if (bool.TryParse(value, out var parsed)) return parsed;

        // Step 3: Parsing failed, return default
        return defaultValue;
    }
}