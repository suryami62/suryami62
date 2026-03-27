#region

using Microsoft.EntityFrameworkCore;
using suryami62.Application.Persistence;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Infrastructure.Persistence;

/// <summary>
///     Persists key/value settings using Entity Framework Core.
/// </summary>
public sealed class SettingsRepository(ApplicationDbContext context) : ISettingsRepository
{
    /// <inheritdoc />
    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        EnsureKey(key);

        return await context.Settings
            .AsNoTracking()
            .Where(setting => setting.Key == key)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> GetValuesAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        if (keys.Count == 0) return CreateEmptyValues();

        return await context.Settings
            .AsNoTracking()
            .Where(setting => keys.Contains(setting.Key))
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, StringComparer.Ordinal,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        EnsureKey(key);
        ArgumentNullException.ThrowIfNull(value);

        await UpsertManyAsync(CreateSingleValueMap(key, value), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpsertManyAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0) return;

        var existingSettingsByKey = await LoadExistingSettingsByKeyAsync(values.Keys, cancellationToken)
            .ConfigureAwait(false);

        UpsertEachSetting(values, existingSettingsByKey);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static Dictionary<string, string> CreateEmptyValues()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private static Dictionary<string, string> CreateSingleValueMap(string key, string value)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [key] = value
        };
    }

    private static void EnsureKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be empty.", nameof(key));
    }

    private async Task<Dictionary<string, Setting>> LoadExistingSettingsByKeyAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken)
    {
        var keyList = keys.ToArray();

        return await context.Settings
            .Where(setting => keyList.Contains(setting.Key))
            .ToDictionaryAsync(setting => setting.Key, StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);
    }

    private void UpsertEachSetting(
        IReadOnlyDictionary<string, string> values,
        Dictionary<string, Setting> existingSettingsByKey)
    {
        foreach (var (key, value) in values)
        {
            if (existingSettingsByKey.TryGetValue(key, out var setting))
            {
                setting.Value = value;
                continue;
            }

            // Reusing the loaded dictionary lets one pass handle both updates and inserts without extra lookups.
            var created = new Setting { Key = key, Value = value };
            context.Settings.Add(created);
            existingSettingsByKey[key] = created;
        }
    }
}