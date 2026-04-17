// ============================================================================
// JOURNEY SETTINGS STORE
// ============================================================================
// This service stores journey/timeline data as JSON in the database settings.
//
// WHAT IS THIS FOR?
// Unlike JourneyHistoryService (which uses a proper database table), this store
// saves journey entries as JSON strings in the settings table. It's used for
// simpler scenarios or when you want to store the entire timeline as one setting.
//
// DATA STRUCTURE:
// Each journey entry contains:
// - Title: Job title or degree name (e.g., "Software Engineer")
// - Organization: Company or school (e.g., "Microsoft")
// - Period: Time range (e.g., "2020 - 2023")
// - Summary: Brief description of the role/studies
// - Highlights: List of achievements or key points
//
// JSON SERIALIZATION:
// JourneyEntry objects are converted to JSON strings for storage:
//   {"title":"Engineer","organization":"Microsoft","period":"2020-2023",...}
// When loaded, JSON is parsed back into JourneyEntry objects.
//
// SETTING KEYS:
// - About:Journey:Experience  - Work history stored as JSON array
// - About:Journey:Education   - Education history stored as JSON array
// ============================================================================

#region

using System.Text.Json;
using suryami62.Application.Persistence;

#endregion

namespace suryami62.Services;

/// <summary>
///     Constants for the setting keys used to store journey data.
///     These are the database keys where JSON data is saved.
/// </summary>
public static class JourneySettingKeys
{
    /// <summary>
    ///     Key for storing work experience entries as JSON.
    ///     Full key: "About:Journey:Experience"
    /// </summary>
    public const string Experience = "About:Journey:Experience";

    /// <summary>
    ///     Key for storing education entries as JSON.
    ///     Full key: "About:Journey:Education"
    /// </summary>
    public const string Education = "About:Journey:Education";
}

/// <summary>
///     A single journey entry (job or education) stored as JSON.
///     This record defines the structure of each timeline item.
/// </summary>
public sealed record JourneyEntry
{
    /// <summary>
    ///     Job title or degree name (e.g., "Software Engineer", "Bachelor of Science").
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    ///     Company name or school (e.g., "Microsoft", "Stanford University").
    /// </summary>
    public string Organization { get; init; } = string.Empty;

    /// <summary>
    ///     Time period (e.g., "2020 - 2023", "2018 - 2022").
    /// </summary>
    public string Period { get; init; } = string.Empty;

    /// <summary>
    ///     Brief description of the role or studies.
    /// </summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    ///     List of achievements or highlights for this entry.
    ///     Example: ["Led team of 5 developers", "Reduced load time by 50%"]
    /// </summary>
    public IReadOnlyList<string> Highlights { get; init; } = new List<string>();
}

/// <summary>
///     All journey content for the About page.
///     Contains both experience (work) and education sections.
/// </summary>
public sealed record AboutJourneySettings
{
    /// <summary>
    ///     Creates a new settings container with the given entries.
    /// </summary>
    /// <param name="experiences">Work experience list.</param>
    /// <param name="educations">Education list.</param>
    public AboutJourneySettings(IReadOnlyList<JourneyEntry> experiences, IReadOnlyList<JourneyEntry> educations)
    {
        Experiences = experiences;
        Educations = educations;
    }

    /// <summary>
    ///     Work experience entries (jobs, internships, volunteer work).
    /// </summary>
    public IReadOnlyList<JourneyEntry> Experiences { get; init; }

    /// <summary>
    ///     Education entries (degrees, certifications, courses).
    /// </summary>
    public IReadOnlyList<JourneyEntry> Educations { get; init; }
}

/// <summary>
///     Loads and saves journey content as JSON in the settings repository.
///     Supports the admin workflow for editing the About page timeline.
/// </summary>
public sealed class JourneySettingsStore
{
    // JSON options for serializing and deserializing journey entries
    // JsonSerializerDefaults.Web uses camelCase property names (e.g., "title" not "Title")
    // PropertyNameCaseInsensitive allows parsing JSON with any case (Title, title, TITLE)
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    // Repository for reading/writing settings to database
    private readonly ISettingsRepository _repository;

    /// <summary>
    ///     Creates a new settings store for journey data.
    /// </summary>
    /// <param name="repository">The settings database repository.</param>
    public JourneySettingsStore(ISettingsRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <summary>
    ///     Loads all journey settings from the database.
    ///     Deserializes JSON strings into JourneyEntry objects.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>Object containing both experience and education lists.</returns>
    public async Task<AboutJourneySettings> GetAsync(CancellationToken cancellationToken = default)
    {
        // Step 1: Load experience entries from settings
        var experiences = await LoadEntriesAsync(
            JourneySettingKeys.Experience,
            cancellationToken).ConfigureAwait(false);

        // Step 2: Load education entries from settings
        var educations = await LoadEntriesAsync(
            JourneySettingKeys.Education,
            cancellationToken).ConfigureAwait(false);

        // Step 3: Return combined settings object
        return new AboutJourneySettings(experiences, educations);
    }

    /// <summary>
    ///     Adds a new work experience entry to the list.
    /// </summary>
    /// <param name="entry">The work experience to add.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    public async Task AddExperienceAsync(JourneyEntry entry, CancellationToken cancellationToken = default)
    {
        await AddEntryAsync(JourneySettingKeys.Experience, entry, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Adds a new education entry to the list.
    /// </summary>
    /// <param name="entry">The education entry to add.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    public async Task AddEducationAsync(JourneyEntry entry, CancellationToken cancellationToken = default)
    {
        await AddEntryAsync(JourneySettingKeys.Education, entry, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Removes a work experience entry by its list index.
    /// </summary>
    /// <param name="index">Zero-based position in the list (0 = first item).</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    public async Task DeleteExperienceAsync(int index, CancellationToken cancellationToken = default)
    {
        await DeleteEntryAsync(JourneySettingKeys.Experience, index, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Removes an education entry by its list index.
    /// </summary>
    /// <param name="index">Zero-based position in the list (0 = first item).</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    public async Task DeleteEducationAsync(int index, CancellationToken cancellationToken = default)
    {
        await DeleteEntryAsync(JourneySettingKeys.Education, index, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Loads and deserializes journey entries from a settings key.
    /// </summary>
    /// <param name="key">The settings key (e.g., "About:Journey:Experience").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>List of journey entries, or empty list if not found/invalid.</returns>
    private async Task<List<JourneyEntry>> LoadEntriesAsync(string key, CancellationToken cancellationToken)
    {
        // Step 1: Read raw JSON string from database
        var rawValue = await _repository.GetValueAsync(key, cancellationToken).ConfigureAwait(false);

        // Step 2: Parse JSON into list of entries
        return DeserializeEntries(rawValue);
    }

    /// <summary>
    ///     Adds a new entry to a journey list and saves back to database.
    /// </summary>
    private async Task AddEntryAsync(
        string settingKey,
        JourneyEntry entry,
        CancellationToken cancellationToken)
    {
        // Step 1: Validate entry is not null
        ArgumentNullException.ThrowIfNull(entry);

        // Step 2: Load existing entries
        var existingEntries = await LoadEntriesAsync(settingKey, cancellationToken)
            .ConfigureAwait(false);

        // Step 3: Add new entry (sanitize cleans up the data)
        existingEntries.Add(Sanitize(entry));

        // Step 4: Save updated list back to database
        await SaveEntriesAsync(settingKey, existingEntries, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Removes an entry by index from a journey list and saves back to database.
    /// </summary>
    private async Task DeleteEntryAsync(
        string settingKey,
        int index,
        CancellationToken cancellationToken)
    {
        // Step 1: Load existing entries
        var existingEntries = await LoadEntriesAsync(settingKey, cancellationToken)
            .ConfigureAwait(false);

        // Step 2: Validate index is valid
        EnsureIndexExists(index, existingEntries.Count);

        // Step 3: Remove the entry at specified index
        existingEntries.RemoveAt(index);

        // Step 4: Save updated list back to database
        await SaveEntriesAsync(settingKey, existingEntries, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Parses a JSON string into a list of JourneyEntry objects.
    ///     Returns empty list if JSON is null, empty, or invalid.
    /// </summary>
    private static List<JourneyEntry> DeserializeEntries(string? rawValue)
    {
        // Step 1: Handle null or empty input
        if (string.IsNullOrWhiteSpace(rawValue)) return new List<JourneyEntry>();

        try
        {
            // Step 2: Parse JSON string into list
            var parsedEntries = JsonSerializer.Deserialize<List<JourneyEntry>>(
                rawValue,
                SerializerOptions);

            // Step 3: Handle null result or empty list
            if (parsedEntries is null || parsedEntries.Count == 0) return new List<JourneyEntry>();

            // Step 4: Sanitize each entry (trim strings, validate)
            return parsedEntries.Select(Sanitize).ToList();
        }
        catch (JsonException)
        {
            // Invalid JSON (corrupted data) - return empty list instead of crashing
            // This allows the admin to recover by re-adding entries
            return new List<JourneyEntry>();
        }
    }

    /// <summary>
    ///     Validates that an index is within valid range for a list.
    ///     Throws exception if index is negative or beyond list size.
    /// </summary>
    private static void EnsureIndexExists(int index, int itemCount)
    {
        if (index < 0 || index >= itemCount) throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <summary>
    ///     Saves a list of entries to the database as a JSON string.
    /// </summary>
    private async Task SaveEntriesAsync(
        string settingKey,
        IReadOnlyList<JourneyEntry> entries,
        CancellationToken cancellationToken)
    {
        // Step 1: Convert list to JSON string
        var payload = JsonSerializer.Serialize(entries, SerializerOptions);

        // Step 2: Save to database (creates if new, updates if exists)
        await _repository.UpsertAsync(settingKey, payload, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Cleans and validates a journey entry before storage or display.
    ///     Trims whitespace from strings and removes empty highlights.
    /// </summary>
    private static JourneyEntry Sanitize(JourneyEntry entry)
    {
        // Step 1: Validate and trim required fields
        var title = RequireTrimmedValue(
            entry.Title,
            nameof(entry),
            "Title is required.");

        var organization = RequireTrimmedValue(
            entry.Organization,
            nameof(entry),
            "Organization is required.");

        var period = RequireTrimmedValue(
            entry.Period,
            nameof(entry),
            "Period is required.");

        // Step 2: Trim summary (optional field, can be empty)
        var summary = entry.Summary?.Trim() ?? string.Empty;

        // Step 3: Clean up highlights list
        // - Trim each highlight string
        // - Remove null/empty/whitespace-only items
        var highlights = entry.Highlights
            .Select(item => item?.Trim() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();

        // Step 4: Return cleaned entry
        return new JourneyEntry
        {
            Title = title,
            Organization = organization,
            Period = period,
            Summary = summary,
            Highlights = highlights
        };
    }

    /// <summary>
    ///     Trims a string and throws exception if it becomes empty.
    ///     Used to validate required fields (Title, Organization, Period).
    /// </summary>
    private static string RequireTrimmedValue(string? value, string paramName, string errorMessage)
    {
        // Step 1: Trim the value (handle null as empty)
        var trimmedValue = value?.Trim() ?? string.Empty;

        // Step 2: Validate not empty after trimming
        if (string.IsNullOrWhiteSpace(trimmedValue)) throw new ArgumentException(errorMessage, paramName);

        return trimmedValue;
    }
}