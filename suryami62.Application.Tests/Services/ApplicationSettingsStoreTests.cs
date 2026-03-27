#region

using suryami62.Services;

#endregion

namespace suryami62.Application.Tests.Services;

public class ApplicationSettingsStoreTests
{
    private readonly FakeSettingsRepository _repository = new();
    private readonly ApplicationSettingsStore _store;

    public ApplicationSettingsStoreTests()
    {
        _store = new ApplicationSettingsStore(_repository);
    }

    [Fact]
    public async Task GetAsyncWhenNoValueStoredReturnsDefaultWithRegistrationEnabled()
    {
        var settings = await _store.GetAsync();

        Assert.True(settings.RegistrationEnabled);
    }

    [Fact]
    public async Task GetAsyncWhenRegistrationIsStoredAsTrueReturnsRegistrationEnabled()
    {
        await _repository.UpsertAsync(ApplicationSettingKeys.RegistrationEnabled, "TRUE");

        var settings = await _store.GetAsync();

        Assert.True(settings.RegistrationEnabled);
    }

    [Fact]
    public async Task GetAsyncWhenRegistrationIsStoredAsFalseReturnsRegistrationDisabled()
    {
        await _repository.UpsertAsync(ApplicationSettingKeys.RegistrationEnabled, "FALSE");

        var settings = await _store.GetAsync();

        Assert.False(settings.RegistrationEnabled);
    }

    [Theory]
    [InlineData("invalid-value")]
    [InlineData("yes")]
    [InlineData("1")]
    [InlineData("0")]
    public async Task GetAsyncWhenValueIsInvalidBooleanReturnsTrueAsDefault(string invalidValue)
    {
        await _repository.UpsertAsync(ApplicationSettingKeys.RegistrationEnabled, invalidValue);

        var settings = await _store.GetAsync();

        Assert.True(settings.RegistrationEnabled);
    }

    [Fact]
    public async Task SetRegistrationEnabledAsyncWithTruePersistsUpperCaseTrue()
    {
        await _store.SetRegistrationEnabledAsync(true);

        var value = await _repository.GetValueAsync(ApplicationSettingKeys.RegistrationEnabled);

        Assert.Equal("TRUE", value);
    }

    [Fact]
    public async Task SetRegistrationEnabledAsyncWithFalsePersistsUpperCaseFalse()
    {
        await _store.SetRegistrationEnabledAsync(false);

        var value = await _repository.GetValueAsync(ApplicationSettingKeys.RegistrationEnabled);

        Assert.Equal("FALSE", value);
    }

    [Fact]
    public async Task GetAsyncAfterSetRegistrationEnabledFalseReturnsRegistrationDisabled()
    {
        await _store.SetRegistrationEnabledAsync(false);

        var settings = await _store.GetAsync();

        Assert.False(settings.RegistrationEnabled);
    }

    [Fact]
    public async Task GetAsyncAfterSetRegistrationEnabledTrueReturnsRegistrationEnabled()
    {
        await _store.SetRegistrationEnabledAsync(false);
        await _store.SetRegistrationEnabledAsync(true);

        var settings = await _store.GetAsync();

        Assert.True(settings.RegistrationEnabled);
    }

    [Fact]
    public void ApplicationSettingsDefaultsHasRegistrationEnabled()
    {
        Assert.True(ApplicationSettings.Defaults.RegistrationEnabled);
    }

    [Fact]
    public void ApplicationSettingKeysRegistrationEnabledHasExpectedValue()
    {
        Assert.Equal("Registration:Enabled", ApplicationSettingKeys.RegistrationEnabled);
    }
}