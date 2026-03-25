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
        var values = await repository.GetValuesAsync(
            [JourneySettingKeys.Experience, JourneySettingKeys.Education],
            cancellationToken).ConfigureAwait(false);

        IReadOnlyList<JourneyEntry> defaultEntries = [];
        var experiences = ReadEntries(values, JourneySettingKeys.Experience, defaultEntries);
        var educations = ReadEntries(values, JourneySettingKeys.Education, defaultEntries);

        return new AboutJourneySettings(experiences, educations);
    }

    /// <summary>
    ///     Appends a new experience entry.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public async Task AddExperienceAsync(JourneyEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var settings = await GetAsync(cancellationToken).ConfigureAwait(false);
        var updated = settings.Experiences.Concat([Sanitize(entry)]).ToList();

        await SaveExperiencesAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Appends a new education entry.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public async Task AddEducationAsync(JourneyEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var settings = await GetAsync(cancellationToken).ConfigureAwait(false);
        var updated = settings.Educations.Concat([Sanitize(entry)]).ToList();

        await SaveEducationsAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Deletes an experience entry by zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index to remove.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public async Task DeleteExperienceAsync(int index, CancellationToken cancellationToken = default)
    {
        var settings = await GetAsync(cancellationToken).ConfigureAwait(false);
        var updated = settings.Experiences.ToList();

        if (index < 0 || index >= updated.Count) throw new ArgumentOutOfRangeException(nameof(index));

        updated.RemoveAt(index);

        await SaveExperiencesAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Deletes an education entry by zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index to remove.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public async Task DeleteEducationAsync(int index, CancellationToken cancellationToken = default)
    {
        var settings = await GetAsync(cancellationToken).ConfigureAwait(false);
        var updated = settings.Educations.ToList();

        if (index < 0 || index >= updated.Count) throw new ArgumentOutOfRangeException(nameof(index));

        updated.RemoveAt(index);

        await SaveEducationsAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Deserializes a journey entry collection from the stored JSON payload for a specific setting key.
    /// </summary>
    /// <param name="values">The settings retrieved from persistence.</param>
    /// <param name="key">The key whose payload should be read.</param>
    /// <param name="fallback">The fallback collection to return when the payload is missing or invalid.</param>
    /// <returns>A sanitized list of journey entries, or <paramref name="fallback" /> when deserialization fails.</returns>
    private static IReadOnlyList<JourneyEntry> ReadEntries(
        IReadOnlyDictionary<string, string> values,
        string key,
        IReadOnlyList<JourneyEntry> fallback)
    {
        if (!values.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;

        try
        {
            var parsed = JsonSerializer.Deserialize<List<JourneyEntry>>(raw, SerializerOptions);
            return parsed is { Count: > 0 } ? parsed.Select(Sanitize).ToList() : [];
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    /// <summary>
    ///     Persists the experience collection back to the settings repository as JSON.
    /// </summary>
    /// <param name="entries">The experience entries to store.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    private async Task SaveExperiencesAsync(IReadOnlyList<JourneyEntry> entries, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(entries, SerializerOptions);
        await repository.UpsertAsync(JourneySettingKeys.Experience, payload, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Persists the education collection back to the settings repository as JSON.
    /// </summary>
    /// <param name="entries">The education entries to store.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    private async Task SaveEducationsAsync(IReadOnlyList<JourneyEntry> entries, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(entries, SerializerOptions);
        await repository.UpsertAsync(JourneySettingKeys.Education, payload, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Normalizes and validates a journey entry before it is persisted or returned to callers.
    /// </summary>
    /// <param name="entry">The entry to sanitize.</param>
    /// <returns>A trimmed and validated journey entry.</returns>
    private static JourneyEntry Sanitize(JourneyEntry entry)
    {
        var title = entry.Title?.Trim() ?? string.Empty;
        var organization = entry.Organization?.Trim() ?? string.Empty;
        var period = entry.Period?.Trim() ?? string.Empty;
        var summary = entry.Summary?.Trim() ?? string.Empty;
        var highlights = entry.Highlights
            .Select(item => item?.Trim() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();

        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(entry));

        if (string.IsNullOrWhiteSpace(organization))
            throw new ArgumentException("Organization is required.", nameof(entry));

        if (string.IsNullOrWhiteSpace(period)) throw new ArgumentException("Period is required.", nameof(entry));

        return new JourneyEntry(title, organization, period, summary, highlights);
    }
}