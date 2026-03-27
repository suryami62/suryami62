#region

using suryami62.Infrastructure.Persistence;

#endregion

namespace suryami62.Infrastructure.Tests.Persistence;

public class SettingsRepositoryTests
{
    [Fact]
    public async Task GetValueAsyncWithExistingKeyReturnsValue()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new SettingsRepository(context);
        await repository.UpsertAsync("TestKey", "TestValue");

        var result = await repository.GetValueAsync("TestKey");

        Assert.Equal("TestValue", result);
    }

    [Fact]
    public async Task GetValueAsyncWithNonExistentKeyReturnsNull()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new SettingsRepository(context);

        var result = await repository.GetValueAsync("Nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetValueAsyncWithEmptyKeyThrowsArgumentException()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new SettingsRepository(context);

        await Assert.ThrowsAsync<ArgumentException>(() => repository.GetValueAsync(string.Empty));
    }

    [Fact]
    public async Task GetValueAsyncWithWhitespaceKeyThrowsArgumentException()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new SettingsRepository(context);

        await Assert.ThrowsAsync<ArgumentException>(() => repository.GetValueAsync("   "));
    }

    [Fact]
    public async Task GetValuesAsyncWithMatchingKeysReturnsValues()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new SettingsRepository(context);
        await repository.UpsertAsync("Key1", "Value1");
        await repository.UpsertAsync("Key2", "Value2");

        var result = await repository.GetValuesAsync(["Key1", "Key2"]);

        Assert.Equal(2, result.Count);
        Assert.Equal("Value1", result["Key1"]);
        Assert.Equal("Value2", result["Key2"]);
    }

    [Fact]
    public async Task GetValuesAsyncWithPartialMatchReturnsOnlyFoundKeys()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new SettingsRepository(context);
        await repository.UpsertAsync("Key1", "Value1");

        var result = await repository.GetValuesAsync(["Key1", "Missing"]);

        Assert.Single(result);
        Assert.True(result.ContainsKey("Key1"));
        Assert.False(result.ContainsKey("Missing"));
    }

    [Fact]
    public async Task GetValuesAsyncWithEmptyKeyCollectionReturnsEmptyDictionary()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new SettingsRepository(context);

        var result = await repository.GetValuesAsync([]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpsertAsyncWithNewKeyCreatesNewSetting()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new SettingsRepository(context);

        await repository.UpsertAsync("NewKey", "NewValue");

        var value = await repository.GetValueAsync("NewKey");
        Assert.Equal("NewValue", value);
    }

    [Fact]
    public async Task UpsertAsyncWithExistingKeyUpdatesValue()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new SettingsRepository(context);
        await repository.UpsertAsync("Key", "OldValue");

        await repository.UpsertAsync("Key", "UpdatedValue");

        var value = await repository.GetValueAsync("Key");
        Assert.Equal("UpdatedValue", value);
        Assert.Single(context.Settings.ToList());
    }

    [Fact]
    public async Task UpsertAsyncWithEmptyKeyThrowsArgumentException()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new SettingsRepository(context);

        await Assert.ThrowsAsync<ArgumentException>(() => repository.UpsertAsync(string.Empty, "value"));
    }

    [Fact]
    public async Task UpsertManyAsyncWithNewKeysCreatesAllSettings()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new SettingsRepository(context);
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["K1"] = "V1",
            ["K2"] = "V2",
            ["K3"] = "V3"
        };

        await repository.UpsertManyAsync(values);

        Assert.Equal(3, context.Settings.ToList().Count);
    }

    [Fact]
    public async Task UpsertManyAsyncWithMixedNewAndExistingCreatesMissingAndUpdatesExisting()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new SettingsRepository(context);
        await repository.UpsertAsync("Existing", "OldValue");

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Existing"] = "NewValue",
            ["NewKey"] = "NewValue"
        };

        await repository.UpsertManyAsync(values);

        Assert.Equal("NewValue", await repository.GetValueAsync("Existing"));
        Assert.Equal("NewValue", await repository.GetValueAsync("NewKey"));
        Assert.Equal(2, context.Settings.ToList().Count);
    }

    [Fact]
    public async Task UpsertManyAsyncWithEmptyDictionaryDoesNothing()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new SettingsRepository(context);

        var exception = await Record.ExceptionAsync(() =>
            repository.UpsertManyAsync(new Dictionary<string, string>(StringComparer.Ordinal)));

        Assert.Null(exception);
        Assert.Empty(context.Settings.ToList());
    }
}