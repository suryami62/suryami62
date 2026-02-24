#region

using Microsoft.EntityFrameworkCore;
using suryami62.Data;
using suryami62.Data.Models;

#endregion

namespace suryami62.Services;

internal static class ApplicationSettingKeys
{
    public const string RegistrationEnabled = "Registration:Enabled";
}

internal sealed record ApplicationSettings(bool RegistrationEnabled)
{
    public static ApplicationSettings Defaults { get; } = new(true);
}

internal sealed class ApplicationSettingsStore(ApplicationDbContext context)
{
    public async Task<ApplicationSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var registrationEnabledSetting = await context.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == ApplicationSettingKeys.RegistrationEnabled, cancellationToken)
            .ConfigureAwait(false);

        var registrationEnabled = registrationEnabledSetting is null ||
                                  !bool.TryParse(registrationEnabledSetting.Value, out var enabled) ||
                                  enabled;

        return new ApplicationSettings(registrationEnabled);
    }

    public async Task SetRegistrationEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        var setting = await context.Settings
            .FirstOrDefaultAsync(s => s.Key == ApplicationSettingKeys.RegistrationEnabled, cancellationToken)
            .ConfigureAwait(false);

        if (setting is null)
        {
            setting = new Setting
            {
                Key = ApplicationSettingKeys.RegistrationEnabled,
                Value = enabled.ToString().ToUpperInvariant()
            };
            context.Settings.Add(setting);
        }
        else
        {
            setting.Value = enabled.ToString().ToUpperInvariant();
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}