#region

using System.Globalization;
using System.Text;
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

        var settingValues = CreateValidatedSettingValues(values);
        var sql = CreateUpsertSql(settingValues.Count);
        var parameters = CreateUpsertParameters(settingValues);

        await _context.Database
            .ExecuteSqlRawAsync(sql, parameters, cancellationToken)
            .ConfigureAwait(false);
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

    private static List<KeyValuePair<string, string>> CreateValidatedSettingValues(
        IReadOnlyDictionary<string, string> values)
    {
        var settingValues = new List<KeyValuePair<string, string>>(values.Count);

        foreach (var (key, value) in values)
        {
            EnsureKey(key);
            ArgumentNullException.ThrowIfNull(value);

            settingValues.Add(new KeyValuePair<string, string>(key, value));
        }

        return settingValues;
    }

    private static string CreateUpsertSql(int settingCount)
    {
        var sql = new StringBuilder();

        sql.AppendLine("""INSERT INTO "Settings" ("Key", "Value")""");
        sql.AppendLine("VALUES");

        for (var index = 0; index < settingCount; index++)
        {
            if (index > 0) sql.AppendLine(",");

            var keyParameterIndex = index * 2;
            var valueParameterIndex = keyParameterIndex + 1;

            sql.Append("({");
            sql.Append(keyParameterIndex.ToString(CultureInfo.InvariantCulture));
            sql.Append("}, {");
            sql.Append(valueParameterIndex.ToString(CultureInfo.InvariantCulture));
            sql.Append("})");
        }

        sql.AppendLine();
        sql.AppendLine("""ON CONFLICT ("Key") DO UPDATE""");
        sql.AppendLine("""SET "Value" = EXCLUDED."Value";""");

        return sql.ToString();
    }

    private static object[] CreateUpsertParameters(List<KeyValuePair<string, string>> settingValues)
    {
        var parameters = new object[settingValues.Count * 2];

        for (var index = 0; index < settingValues.Count; index++)
        {
            var keyParameterIndex = index * 2;
            var valueParameterIndex = keyParameterIndex + 1;

            parameters[keyParameterIndex] = settingValues[index].Key;
            parameters[valueParameterIndex] = settingValues[index].Value;
        }

        return parameters;
    }
}