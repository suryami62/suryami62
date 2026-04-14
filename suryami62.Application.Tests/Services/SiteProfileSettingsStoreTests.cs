#region

using suryami62.Services;

#endregion

namespace suryami62.Application.Tests.Services;

public class SiteProfileSettingsStoreTests
{
    private readonly FakeSettingsRepository _repository = new();
    private readonly SiteProfileSettingsStore _store;

    public SiteProfileSettingsStoreTests()
    {
        _store = new SiteProfileSettingsStore(_repository);
    }

    [Fact]
    public async Task GetAsyncWhenNoValuesStoredReturnsAllEmptyStrings()
    {
        var settings = await _store.GetAsync();

        Assert.Equal(string.Empty, settings.Instagram);
        Assert.Equal(string.Empty, settings.Linkedin);
        Assert.Equal(string.Empty, settings.Github);
        Assert.Equal(string.Empty, settings.Email);
    }

    [Fact]
    public async Task GetAsyncWhenAllValuesStoredReturnsStoredValues()
    {
        await _repository.UpsertAsync(SiteProfileSettingKeys.Instagram, "myInstagram");
        await _repository.UpsertAsync(SiteProfileSettingKeys.Linkedin, "linkedin.com/in/me");
        await _repository.UpsertAsync(SiteProfileSettingKeys.Github, "github.com/me");
        await _repository.UpsertAsync(SiteProfileSettingKeys.Email, "me@example.com");

        var settings = await _store.GetAsync();

        Assert.Equal("myInstagram", settings.Instagram);
        Assert.Equal("linkedin.com/in/me", settings.Linkedin);
        Assert.Equal("github.com/me", settings.Github);
        Assert.Equal("me@example.com", settings.Email);
    }

    [Fact]
    public async Task GetAsyncWhenOnlySomeValuesStoredReturnsMixedValues()
    {
        await _repository.UpsertAsync(SiteProfileSettingKeys.Github, "github.com/me");

        var settings = await _store.GetAsync();

        Assert.Equal(string.Empty, settings.Instagram);
        Assert.Equal("github.com/me", settings.Github);
    }

    [Fact]
    public async Task SaveAsyncPersistsAllValues()
    {
        var profile = new SiteProfileSettings
        {
            Instagram = "insta",
            Linkedin = "linkedin",
            Github = "github",
            Email = "email@test.com"
        };

        await _store.SaveAsync(profile);

        var loaded = await _store.GetAsync();
        Assert.Equal("insta", loaded.Instagram);
        Assert.Equal("linkedin", loaded.Linkedin);
        Assert.Equal("github", loaded.Github);
        Assert.Equal("email@test.com", loaded.Email);
    }

    [Fact]
    public async Task SaveAsyncWithNullSettingsThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _store.SaveAsync(null!));
    }

    [Fact]
    public async Task SaveAsyncCalledTwiceOverwritesPreviousValues()
    {
        await _store.SaveAsync(new SiteProfileSettings
        {
            Instagram = "old",
            Linkedin = "old",
            Github = "old",
            Email = "old@test.com"
        });
        await _store.SaveAsync(new SiteProfileSettings
        {
            Instagram = "new",
            Linkedin = "new",
            Github = "new",
            Email = "new@test.com"
        });

        var settings = await _store.GetAsync();

        Assert.Equal("new", settings.Instagram);
        Assert.Equal("new", settings.Linkedin);
    }

    [Fact]
    public void SiteProfileSettingsDefaultsHasAllEmptyStrings()
    {
        var defaults = SiteProfileSettings.Defaults;

        Assert.Equal(string.Empty, defaults.Instagram);
        Assert.Equal(string.Empty, defaults.Linkedin);
        Assert.Equal(string.Empty, defaults.Github);
        Assert.Equal(string.Empty, defaults.Email);
    }

    [Fact]
    public void SiteProfileSettingKeysHaveExpectedValues()
    {
        Assert.Equal("UserInfo:Instagram", SiteProfileSettingKeys.Instagram);
        Assert.Equal("UserInfo:Linkedin", SiteProfileSettingKeys.Linkedin);
        Assert.Equal("UserInfo:Github", SiteProfileSettingKeys.Github);
        Assert.Equal("UserInfo:Email", SiteProfileSettingKeys.Email);
    }
}