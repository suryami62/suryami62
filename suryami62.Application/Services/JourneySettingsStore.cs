#region

using System.Text.Json;
using suryami62.Application.Persistence;

#endregion

namespace suryami62.Services;

/// <summary>
///     Declares setting keys used to store serialized journey content.
/// </summary>
public static class JourneySettingKeys
{
    /// <summary>
    ///     The key that stores experience entries.
    /// </summary>
    public const string Experience = "About:Journey:Experience";

    /// <summary>
    ///     The key that stores education entries.
    /// </summary>
    public const string Education = "About:Journey:Education";
}

/// <summary>
///     Represents a single journey entry persisted in JSON settings.
/// </summary>
public sealed record JourneyEntry(
    string Title,
    string Organization,
    string Period,
    string Summary,
    IReadOnlyList<string> Highlights);

/// <summary>
///     Represents all journey content rendered on the about page.
/// </summary>
public sealed record AboutJourneySettings(
    IReadOnlyList<JourneyEntry> Experiences,
    IReadOnlyList<JourneyEntry> Educations);

/// <summary>
///     Loads and persists journey content in the settings repository.
/// </summary>
/// <remarks>
///     This store supports the admin workflow for editing the About page timeline while keeping persistence in the
///     generic settings table through JSON serialization.
/// </remarks>
public sealed class JourneySettingsStore(ISettingsRepository repository)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     Loads the current journey settings from persisted storage.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The deserialized journey settings with safe fallbacks.</returns>
    public async Task<AboutJourneySettings> GetAsync(CancellationToken cancellationToken = default)
    {
        return new AboutJourneySettings(
            await LoadEntriesAsync(JourneySettingKeys.Experience, cancellationToken).ConfigureAwait(false),
            await LoadEntriesAsync(JourneySettingKeys.Education, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    ///     Appends a new experience entry.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public async Task AddExperienceAsync(JourneyEntry entry, CancellationToken cancellationToken = default)
    {
        await AddEntryAsync(JourneySettingKeys.Experience, entry, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Appends a new education entry.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public async Task AddEducationAsync(JourneyEntry entry, CancellationToken cancellationToken = default)
    {
        await AddEntryAsync(JourneySettingKeys.Education, entry, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Deletes an experience entry by zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index to remove.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public async Task DeleteExperienceAsync(int index, CancellationToken cancellationToken = default)
    {
        await DeleteEntryAsync(JourneySettingKeys.Experience, index, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Deletes an education entry by zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index to remove.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public async Task DeleteEducationAsync(int index, CancellationToken cancellationToken = default)
    {
        await DeleteEntryAsync(JourneySettingKeys.Education, index, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Loads and deserializes one journey collection from storage.
    /// </summary>
    /// <param name="key">The key whose payload should be read.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A sanitized list of journey entries, or an empty list when storage is missing or invalid.</returns>
    private async Task<List<JourneyEntry>> LoadEntriesAsync(string key, CancellationToken cancellationToken)
    {
        var rawValue = await repository.GetValueAsync(key, cancellationToken).ConfigureAwait(false);
        return DeserializeEntries(rawValue);
    }

    private async Task AddEntryAsync(
        string settingKey,
        JourneyEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var existingEntries = await LoadEntriesAsync(settingKey, cancellationToken).ConfigureAwait(false);
        existingEntries.Add(Sanitize(entry));

        await SaveEntriesAsync(settingKey, existingEntries, cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteEntryAsync(
        string settingKey,
        int index,
        CancellationToken cancellationToken)
    {
        var existingEntries = await LoadEntriesAsync(settingKey, cancellationToken).ConfigureAwait(false);
        EnsureIndexExists(index, existingEntries.Count);

        existingEntries.RemoveAt(index);
        await SaveEntriesAsync(settingKey, existingEntries, cancellationToken).ConfigureAwait(false);
    }

    private static List<JourneyEntry> DeserializeEntries(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue)) return [];

        try
        {
            var parsedEntries = JsonSerializer.Deserialize<List<JourneyEntry>>(rawValue, SerializerOptions);

            if (parsedEntries is null || parsedEntries.Count == 0) return [];

            return parsedEntries.Select(Sanitize).ToList();
        }
        catch (JsonException)
        {
            // Invalid admin JSON should not break the About page; returning an empty list keeps editing recoverable.
            return [];
        }
    }

    private static void EnsureIndexExists(int index, int itemCount)
    {
        if (index < 0 || index >= itemCount) throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <summary>
    ///     Persists one journey collection back to the settings repository as JSON.
    /// </summary>
    /// <param name="settingKey">The setting key that owns the collection.</param>
    /// <param name="entries">The entries to store.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    private async Task SaveEntriesAsync(
        string settingKey,
        IReadOnlyList<JourneyEntry> entries,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(entries, SerializerOptions);
        await repository.UpsertAsync(settingKey, payload, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Normalizes and validates a journey entry before it is persisted or returned to callers.
    /// </summary>
    /// <param name="entry">The entry to sanitize.</param>
    /// <returns>A trimmed and validated journey entry.</returns>
    private static JourneyEntry Sanitize(JourneyEntry entry)
    {
        var title = RequireTrimmedValue(entry.Title, nameof(entry), "Title is required.");
        var organization = RequireTrimmedValue(entry.Organization, nameof(entry), "Organization is required.");
        var period = RequireTrimmedValue(entry.Period, nameof(entry), "Period is required.");
        var summary = entry.Summary?.Trim() ?? string.Empty;
        var highlights = entry.Highlights
            .Select(item => item?.Trim() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();

        // Trimming once at the boundary keeps the rest of the application free from repeated cleanup logic.
        return new JourneyEntry(title, organization, period, summary, highlights);
    }

    private static string RequireTrimmedValue(string? value, string paramName, string errorMessage)
    {
        var trimmedValue = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedValue)) throw new ArgumentException(errorMessage, paramName);

        return trimmedValue;
    }
}