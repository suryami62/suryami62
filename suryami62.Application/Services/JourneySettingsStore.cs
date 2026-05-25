#region

using System.Text.Json;
using suryami62.Application.Persistence;

#endregion

namespace suryami62.Services;

public static class JourneySettingKeys
{
    public const string Experience = "About:Journey:Experience";

    public const string Education = "About:Journey:Education";
}

public sealed record JourneyEntry
{
    public string Title { get; init; } = string.Empty;

    public string Organization { get; init; } = string.Empty;

    public string Period { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public IReadOnlyList<string> Highlights { get; init; } = new List<string>();
}

public sealed record AboutJourneySettings
{
    public AboutJourneySettings(IReadOnlyList<JourneyEntry> experiences, IReadOnlyList<JourneyEntry> educations)
    {
        Experiences = experiences;
        Educations = educations;
    }

    public IReadOnlyList<JourneyEntry> Experiences { get; init; }

    public IReadOnlyList<JourneyEntry> Educations { get; init; }
}

public sealed class JourneySettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ISettingsRepository _repository;

    public JourneySettingsStore(ISettingsRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<AboutJourneySettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var experiences = await LoadEntriesAsync(
            JourneySettingKeys.Experience,
            cancellationToken).ConfigureAwait(false);

        var educations = await LoadEntriesAsync(
            JourneySettingKeys.Education,
            cancellationToken).ConfigureAwait(false);

        return new AboutJourneySettings(experiences, educations);
    }

    public async Task AddExperienceAsync(JourneyEntry entry, CancellationToken cancellationToken = default)
    {
        await AddEntryAsync(JourneySettingKeys.Experience, entry, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddEducationAsync(JourneyEntry entry, CancellationToken cancellationToken = default)
    {
        await AddEntryAsync(JourneySettingKeys.Education, entry, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteExperienceAsync(int index, CancellationToken cancellationToken = default)
    {
        await DeleteEntryAsync(JourneySettingKeys.Experience, index, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteEducationAsync(int index, CancellationToken cancellationToken = default)
    {
        await DeleteEntryAsync(JourneySettingKeys.Education, index, cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<JourneyEntry>> LoadEntriesAsync(string key, CancellationToken cancellationToken)
    {
        var rawValue = await _repository.GetValueAsync(key, cancellationToken).ConfigureAwait(false);

        return DeserializeEntries(rawValue);
    }

    private async Task AddEntryAsync(
        string settingKey,
        JourneyEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var existingEntries = await LoadEntriesAsync(settingKey, cancellationToken)
            .ConfigureAwait(false);

        existingEntries.Add(Sanitize(entry));

        await SaveEntriesAsync(settingKey, existingEntries, cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteEntryAsync(
        string settingKey,
        int index,
        CancellationToken cancellationToken)
    {
        var existingEntries = await LoadEntriesAsync(settingKey, cancellationToken)
            .ConfigureAwait(false);

        EnsureIndexExists(index, existingEntries.Count);

        existingEntries.RemoveAt(index);

        await SaveEntriesAsync(settingKey, existingEntries, cancellationToken).ConfigureAwait(false);
    }

    private static List<JourneyEntry> DeserializeEntries(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue)) return new List<JourneyEntry>();

        try
        {
            var parsedEntries = JsonSerializer.Deserialize<List<JourneyEntry>>(
                rawValue,
                SerializerOptions);

            if (parsedEntries is null || parsedEntries.Count == 0) return new List<JourneyEntry>();

            return parsedEntries.Select(Sanitize).ToList();
        }
        catch (JsonException)
        {
            return new List<JourneyEntry>();
        }
    }

    private static void EnsureIndexExists(int index, int itemCount)
    {
        if (index < 0 || index >= itemCount) throw new ArgumentOutOfRangeException(nameof(index));
    }

    private async Task SaveEntriesAsync(
        string settingKey,
        IReadOnlyList<JourneyEntry> entries,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(entries, SerializerOptions);

        await _repository.UpsertAsync(settingKey, payload, cancellationToken).ConfigureAwait(false);
    }

    private static JourneyEntry Sanitize(JourneyEntry entry)
    {
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

        var summary = entry.Summary?.Trim() ?? string.Empty;

        var highlights = entry.Highlights
            .Select(item => item?.Trim() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();

        return new JourneyEntry
        {
            Title = title,
            Organization = organization,
            Period = period,
            Summary = summary,
            Highlights = highlights
        };
    }

    private static string RequireTrimmedValue(string? value, string paramName, string errorMessage)
    {
        var trimmedValue = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedValue)) throw new ArgumentException(errorMessage, paramName);

        return trimmedValue;
    }
}