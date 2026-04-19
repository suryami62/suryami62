#region

using suryami62.Services;

#endregion

namespace suryami62.Application.Tests.Services;

public class JourneySettingsStoreTests
{
    private readonly FakeSettingsRepository _repository = new();
    private readonly JourneySettingsStore _store;

    public JourneySettingsStoreTests()
    {
        _store = new JourneySettingsStore(_repository);
    }

    private static JourneyEntry CreateEntry(
        string title = "Title",
        string organization = "Org",
        string period = "2020-2023",
        string summary = "Summary")
    {
        return new JourneyEntry
            { Title = title, Organization = organization, Period = period, Summary = summary, Highlights = [] };
    }

    [Fact]
    public async Task GetAsyncWhenNoDataReturnsEmptyExperiences()
    {
        var settings = await _store.GetAsync();

        Assert.Empty(settings.Experiences);
    }

    [Fact]
    public async Task GetAsyncWhenNoDataReturnsEmptyEducations()
    {
        var settings = await _store.GetAsync();

        Assert.Empty(settings.Educations);
    }

    [Fact]
    public async Task AddExperienceAsyncWithValidEntryAppendsEntry()
    {
        var entry = CreateEntry("Software Engineer", "Company A");

        await _store.AddExperienceAsync(entry);

        var settings = await _store.GetAsync();
        Assert.Single(settings.Experiences);
        Assert.Equal("Software Engineer", settings.Experiences[0].Title);
    }

    [Fact]
    public async Task AddExperienceAsyncCalledTwiceAppendsBothEntries()
    {
        await _store.AddExperienceAsync(CreateEntry("First Role"));
        await _store.AddExperienceAsync(CreateEntry("Second Role"));

        var settings = await _store.GetAsync();

        Assert.Equal(2, settings.Experiences.Count);
        Assert.Equal("First Role", settings.Experiences[0].Title);
        Assert.Equal("Second Role", settings.Experiences[1].Title);
    }

    [Fact]
    public async Task AddEducationAsyncWithValidEntryAppendsEntry()
    {
        var entry = CreateEntry("Computer Science", "University X", "2016-2020");

        await _store.AddEducationAsync(entry);

        var settings = await _store.GetAsync();
        Assert.Single(settings.Educations);
        Assert.Equal("Computer Science", settings.Educations[0].Title);
    }

    [Fact]
    public async Task AddExperienceAsyncWithNullEntryThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _store.AddExperienceAsync(null!));
    }

    [Fact]
    public async Task AddEducationAsyncWithNullEntryThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _store.AddEducationAsync(null!));
    }

    [Fact]
    public async Task DeleteExperienceAsyncWithValidIndexRemovesEntry()
    {
        await _store.AddExperienceAsync(CreateEntry("First Role"));
        await _store.AddExperienceAsync(CreateEntry("Second Role"));

        await _store.DeleteExperienceAsync(0);

        var settings = await _store.GetAsync();
        Assert.Single(settings.Experiences);
        Assert.Equal("Second Role", settings.Experiences[0].Title);
    }

    [Fact]
    public async Task DeleteExperienceAsyncWithNegativeIndexThrowsArgumentOutOfRangeException()
    {
        await _store.AddExperienceAsync(CreateEntry());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _store.DeleteExperienceAsync(-1));
    }

    [Fact]
    public async Task DeleteExperienceAsyncWithIndexEqualToCountThrowsArgumentOutOfRangeException()
    {
        await _store.AddExperienceAsync(CreateEntry());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _store.DeleteExperienceAsync(1));
    }

    [Fact]
    public async Task DeleteExperienceAsyncOnEmptyListThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _store.DeleteExperienceAsync(0));
    }

    [Fact]
    public async Task DeleteEducationAsyncWithValidIndexRemovesEntry()
    {
        await _store.AddEducationAsync(CreateEntry("CS Degree", "University"));
        await _store.AddEducationAsync(CreateEntry("MBA", "Business School"));

        await _store.DeleteEducationAsync(1);

        var settings = await _store.GetAsync();
        Assert.Single(settings.Educations);
        Assert.Equal("CS Degree", settings.Educations[0].Title);
    }

    [Fact]
    public async Task DeleteEducationAsyncOnEmptyListThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _store.DeleteEducationAsync(0));
    }

    [Fact]
    public async Task AddExperienceAndAddEducationAreStoredIndependently()
    {
        await _store.AddExperienceAsync(CreateEntry("Engineer"));
        await _store.AddEducationAsync(CreateEntry("CS Degree"));

        var settings = await _store.GetAsync();

        Assert.Single(settings.Experiences);
        Assert.Single(settings.Educations);
        Assert.Equal("Engineer", settings.Experiences[0].Title);
        Assert.Equal("CS Degree", settings.Educations[0].Title);
    }

    [Fact]
    public async Task GetAsyncWhenStoredJsonIsInvalidReturnsEmptyCollections()
    {
        await _repository.UpsertAsync(JourneySettingKeys.Experience, "not-json");
        await _repository.UpsertAsync(JourneySettingKeys.Education, "{broken");

        var settings = await _store.GetAsync();

        Assert.Empty(settings.Experiences);
        Assert.Empty(settings.Educations);
    }

    [Fact]
    public async Task AddExperienceAsyncWithWhitespaceInputStoresSanitizedEntry()
    {
        var entry = new JourneyEntry
        {
            Title = "  Software Engineer  ",
            Organization = "  Company A  ",
            Period = "  2020-2023  ",
            Summary = "  Built internal tools.  ",
            Highlights = ["  APIs  ", " ", "  Mentoring  "]
        };

        await _store.AddExperienceAsync(entry);

        var settings = await _store.GetAsync();
        var storedEntry = Assert.Single(settings.Experiences);

        Assert.Equal("Software Engineer", storedEntry.Title);
        Assert.Equal("Company A", storedEntry.Organization);
        Assert.Equal("2020-2023", storedEntry.Period);
        Assert.Equal("Built internal tools.", storedEntry.Summary);
        Assert.Equal(["APIs", "Mentoring"], storedEntry.Highlights);
    }

    [Fact]
    public void JourneySettingKeysHaveExpectedValues()
    {
        Assert.Equal("About:Journey:Experience", JourneySettingKeys.Experience);
        Assert.Equal("About:Journey:Education", JourneySettingKeys.Education);
    }
}