// ============================================================================
// SETTINGS REPOSITORY
// ============================================================================
// This class implements key-value settings storage using Entity Framework Core.
// Settings are stored as simple key-value pairs in the database.
//
// WHAT IS "UPSERT"?
// "Upsert" = Update or Insert (a portmanteau of Update + Insert).
// If the key exists, update its value. If not, create a new entry.
//
// BATCH OPERATIONS:
// UpsertManyAsync handles multiple settings in one database transaction.
// This is more efficient than calling UpsertAsync() for each setting individually.
//
// BATCH ALGORITHM:
// 1. Load all existing settings with matching keys in ONE query
// 2. For each key-value pair:
//    - If exists in loaded set: UPDATE the existing Setting entity
//    - If not exists: CREATE new Setting entity and add to context
// 3. Save all changes in ONE transaction
//
// DICTIONARY RETURN TYPE:
// GetValuesAsync returns IReadOnlyDictionary (read-only view).
// Callers cannot accidentally modify the returned dictionary.
// StringComparer.Ordinal = case-sensitive, culture-neutral key comparison.
// ============================================================================

#region

using Microsoft.EntityFrameworkCore;
using suryami62.Application.Persistence;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Infrastructure.Persistence;

/// <summary>
///     Implements key-value settings data access using Entity Framework Core.
///     Handles database operations for the Settings table.
/// </summary>
public sealed class SettingsRepository : ISettingsRepository
{
    // The database context - provides access to the Settings table
    private readonly ApplicationDbContext _context;

    /// <summary>
    ///     Creates a new settings repository with the given database context.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    public SettingsRepository(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <summary>
    ///     Gets a single setting value by its key.
    /// </summary>
    /// <param name="key">The setting key to look up.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The setting value, or null if not found.</returns>
    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        // Step 1: Validate key is not empty
        EnsureKey(key);

        // Step 2: Query database for this key
        // AsNoTracking(): Read-only, no change tracking (faster)
        // Select(): Only return the Value column (not full Setting entity)
        // FirstOrDefaultAsync(): Return value if found, null if not
        var value = await _context.Settings
            .AsNoTracking()
            .Where(setting => setting.Key == key)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return value;
    }

    /// <summary>
    ///     Gets multiple setting values by their keys (batch operation).
    ///     More efficient than calling GetValueAsync multiple times.
    /// </summary>
    /// <param name="keys">The list of keys to look up.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Read-only dictionary of key-value pairs (only found keys).</returns>
    public async Task<IReadOnlyDictionary<string, string>> GetValuesAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Validate input
        ArgumentNullException.ThrowIfNull(keys);

        // Step 2: Handle empty key list (return empty dictionary)
        if (keys.Count == 0) return CreateEmptyValues();

        // Step 3: Query database for all matching keys in ONE query
        // keys.Contains() generates SQL: WHERE Key IN ('key1', 'key2', ...)
        // ToDictionaryAsync() maps results to Dictionary<string, string>
        // StringComparer.Ordinal = case-sensitive key comparison
        var results = await _context.Settings
            .AsNoTracking()
            .Where(setting => keys.Contains(setting.Key))
            .ToDictionaryAsync(
                setting => setting.Key,
                setting => setting.Value,
                StringComparer.Ordinal,
                cancellationToken)
            .ConfigureAwait(false);

        // Note: If some keys don't exist, they are simply not in the result.
        // Callers should check dictionary.ContainsKey() to detect missing values.
        return results;
    }

    /// <summary>
    ///     Creates or updates a single setting (upsert operation).
    ///     Delegates to UpsertManyAsync for consistent batch handling.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        // Step 1: Validate inputs
        EnsureKey(key);
        ArgumentNullException.ThrowIfNull(value);

        // Step 2: Convert to single-entry dictionary and use batch method
        // This ensures consistent handling with UpsertManyAsync
        var singleValueMap = CreateSingleValueMap(key, value);
        await UpsertManyAsync(singleValueMap, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Creates or updates multiple settings in one database transaction (batch upsert).
    ///     This is the main implementation - UpsertAsync delegates to this method.
    /// </summary>
    /// <param name="values">Dictionary of key-value pairs to upsert.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task UpsertManyAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Validate input
        ArgumentNullException.ThrowIfNull(values);

        // Step 2: Handle empty dictionary (nothing to do)
        if (values.Count == 0) return;

        // Step 3: Load all existing settings that match our keys
        // This ONE query gets us all current values we might need to update
        var existingSettingsByKey = await LoadExistingSettingsByKeyAsync(
                values.Keys,
                cancellationToken)
            .ConfigureAwait(false);

        // Step 4: For each key-value pair, decide UPDATE or INSERT
        // This modifies EF Core's change tracker (no database calls yet)
        UpsertEachSetting(values, existingSettingsByKey);

        // Step 5: Save all changes (INSERTs and UPDATEs) in ONE transaction
        // SaveChangesAsync generates SQL and executes it atomically
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Creates an empty dictionary with Ordinal string comparer.
    ///     Used as return value when input key list is empty.
    /// </summary>
    private static Dictionary<string, string> CreateEmptyValues()
    {
        // StringComparer.Ordinal = case-sensitive, culture-neutral
        // "Key" and "key" are treated as different keys
        return new Dictionary<string, string>(StringComparer.Ordinal);
    }

    /// <summary>
    ///     Creates a single-entry dictionary from a key-value pair.
    ///     Used by UpsertAsync to convert to batch format.
    /// </summary>
    private static Dictionary<string, string> CreateSingleValueMap(string key, string value)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [key] = value
        };
    }

    /// <summary>
    ///     Validates that a key is not null, empty, or whitespace.
    ///     Throws ArgumentException if validation fails.
    /// </summary>
    private static void EnsureKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be empty.", nameof(key));
    }

    /// <summary>
    ///     Loads all existing settings matching the given keys from database.
    ///     Returns as a dictionary for O(1) lookup performance.
    /// </summary>
    private async Task<Dictionary<string, Setting>> LoadExistingSettingsByKeyAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken)
    {
        // Convert to array for use in Contains() query
        // IEnumerable might be evaluated multiple times, array ensures single enumeration
        var keyList = keys.ToArray();

        // Query database for settings with matching keys
        // ToDictionaryAsync maps results to Dictionary<string, Setting>
        // Key = setting.Key, Value = the full Setting entity
        var existingSettings = await _context.Settings
            .Where(setting => keyList.Contains(setting.Key))
            .ToDictionaryAsync(
                setting => setting.Key,
                StringComparer.Ordinal,
                cancellationToken)
            .ConfigureAwait(false);

        return existingSettings;
    }

    /// <summary>
    ///     Processes each key-value pair and marks it for update or insert.
    ///     Modifies EF Core's change tracker; no database calls in this method.
    /// </summary>
    private void UpsertEachSetting(
        IReadOnlyDictionary<string, string> values,
        Dictionary<string, Setting> existingSettingsByKey)
    {
        // Iterate through all key-value pairs we need to upsert
        foreach (var (key, value) in values)
        {
            // Check if this key already exists in database
            if (existingSettingsByKey.TryGetValue(key, out var existingSetting))
            {
                // CASE 1: UPDATE - Key exists, just update the value
                // EF Core tracks this Setting entity, will generate UPDATE SQL
                existingSetting.Value = value;
                continue; // Move to next key-value pair
            }

            // CASE 2: INSERT - Key doesn't exist, create new Setting
            // Create new entity (not yet tracked by EF Core)
            var newSetting = new Setting { Key = key, Value = value };

            // Add to context - EF Core will track and generate INSERT SQL
            _context.Settings.Add(newSetting);

            // Add to our dictionary so subsequent keys don't create duplicates
            // (if same key appears twice in values, second would find this new one)
            existingSettingsByKey[key] = newSetting;
        }
    }
}