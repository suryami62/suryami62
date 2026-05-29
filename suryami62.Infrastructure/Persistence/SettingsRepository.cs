#region

using Microsoft.EntityFrameworkCore;
using suryami62.Application.Persistence;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Infrastructure.Persistence;

public sealed class SettingsRepository : ISettingsRepository
{
    private readonly ApplicationDbContext _context;

    public SettingsRepository(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        EnsureKey(key);

        var value = await _context.Settings
            .AsNoTracking()
            .Where(setting => setting.Key == key)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return value;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetValuesAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        if (keys.Count == 0) return CreateEmptyValues();

        var results = await _context.Settings
            .AsNoTracking()
            .Where(setting => keys.Contains(setting.Key))
            .ToDictionaryAsync(
                setting => setting.Key,
                setting => setting.Value,
                StringComparer.Ordinal,
                cancellationToken)
            .ConfigureAwait(false);

        return results;
    }

    public async Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        EnsureKey(key);
        ArgumentNullException.ThrowIfNull(value);

        var singleValueMap = CreateSingleValueMap(key, value);
        await UpsertManyAsync(singleValueMap, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertManyAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0) return;

        var existingSettingsByKey = await LoadExistingSettingsByKeyAsync(
                values.Keys,
                cancellationToken)
            .ConfigureAwait(false);

        UpsertEachSetting(values, existingSettingsByKey);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

        if (key.Length > DomainModelConstraints.SettingKeyMaxLength)
        {
            throw new ArgumentException(
                $"Key cannot exceed {DomainModelConstraints.SettingKeyMaxLength} characters.",
                nameof(key));
        }
    }

    private async Task<Dictionary<string, Setting>> LoadExistingSettingsByKeyAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken)
    {
        var keyList = keys.ToArray();

        var existingSettings = await _context.Settings
            .Where(setting => keyList.Contains(setting.Key))
            .ToDictionaryAsync(
                setting => setting.Key,
                StringComparer.Ordinal,
                cancellationToken)
            .ConfigureAwait(false);

        return existingSettings;
    }

    private void UpsertEachSetting(
        IReadOnlyDictionary<string, string> values,
        Dictionary<string, Setting> existingSettingsByKey)
    {
        foreach (var (key, value) in values)
        {
            if (existingSettingsByKey.TryGetValue(key, out var existingSetting))
            {
                existingSetting.Value = value;
                continue;
            }

            var newSetting = new Setting { Key = key, Value = value };

            _context.Settings.Add(newSetting);

            existingSettingsByKey[key] = newSetting;
        }
    }
}