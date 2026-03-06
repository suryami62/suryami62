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

public sealed record JourneyEntry(
    string Title,
    string Organization,
    string Period,
    string Summary,
    IReadOnlyList<string> Highlights);

public sealed record AboutJourneySettings(
    IReadOnlyList<JourneyEntry> Experiences,
    IReadOnlyList<JourneyEntry> Educations);

public sealed class JourneySettingsStore(ISettingsRepository repository)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

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

    public async Task AddExperienceAsync(JourneyEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var settings = await GetAsync(cancellationToken).ConfigureAwait(false);
        var updated = settings.Experiences.Concat([Sanitize(entry)]).ToList();

        await SaveExperiencesAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddEducationAsync(JourneyEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var settings = await GetAsync(cancellationToken).ConfigureAwait(false);
        var updated = settings.Educations.Concat([Sanitize(entry)]).ToList();

        await SaveEducationsAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteExperienceAsync(int index, CancellationToken cancellationToken = default)
    {
        var settings = await GetAsync(cancellationToken).ConfigureAwait(false);
        var updated = settings.Experiences.ToList();

        if (index < 0 || index >= updated.Count) throw new ArgumentOutOfRangeException(nameof(index));

        updated.RemoveAt(index);

        await SaveExperiencesAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteEducationAsync(int index, CancellationToken cancellationToken = default)
    {
        var settings = await GetAsync(cancellationToken).ConfigureAwait(false);
        var updated = settings.Educations.ToList();

        if (index < 0 || index >= updated.Count) throw new ArgumentOutOfRangeException(nameof(index));

        updated.RemoveAt(index);

        await SaveEducationsAsync(updated, cancellationToken).ConfigureAwait(false);
    }

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

    private async Task SaveExperiencesAsync(IReadOnlyList<JourneyEntry> entries, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(entries, SerializerOptions);
        await repository.UpsertAsync(JourneySettingKeys.Experience, payload, cancellationToken).ConfigureAwait(false);
    }

    private async Task SaveEducationsAsync(IReadOnlyList<JourneyEntry> entries, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(entries, SerializerOptions);
        await repository.UpsertAsync(JourneySettingKeys.Education, payload, cancellationToken).ConfigureAwait(false);
    }

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