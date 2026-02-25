#region

using Microsoft.EntityFrameworkCore;
using suryami62.Application.Persistence;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Infrastructure.Persistence;

internal sealed class SettingsRepository(ApplicationDbContext context) : ISettingsRepository
{
    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

        return await context.Settings
            .AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetValuesAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        if (keys.Count == 0) return new Dictionary<string, string>(StringComparer.Ordinal);

        return await context.Settings
            .AsNoTracking()
            .Where(s => keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value, StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

        ArgumentNullException.ThrowIfNull(value);

        await UpsertManyAsync(new Dictionary<string, string>(StringComparer.Ordinal) { [key] = value },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertManyAsync(IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0) return;

        var keys = values.Keys.ToArray();

        var existing = await context.Settings
            .Where(s => keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);

        foreach (var (key, value) in values)
        {
            if (existing.TryGetValue(key, out var setting))
            {
                setting.Value = value;
                continue;
            }

            var created = new Setting { Key = key, Value = value };
            context.Settings.Add(created);
            existing[key] = created;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}