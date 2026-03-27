#region

using suryami62.Domain.Models;

#endregion

namespace suryami62.Domain.Tests.Models;

public class SettingTests
{
    [Fact]
    public void Constructor_WhenCreated_SetsExpectedDefaultValues()
    {
        var setting = new Setting();

        Assert.Equal(string.Empty, setting.Key);
        Assert.Equal(string.Empty, setting.Value);
    }

    [Theory]
    [InlineData("Registration:Enabled")]
    [InlineData("UserInfo:Email")]
    [InlineData("About:Journey:Experience")]
    [InlineData("UserInfo:Github")]
    public void Key_WhenAssigned_PreservesValue(string key)
    {
        var setting = new Setting { Key = key };
        Assert.Equal(key, setting.Key);
    }

    [Theory]
    [InlineData("TRUE")]
    [InlineData("FALSE")]
    [InlineData("some-value")]
    [InlineData("")]
    public void Value_WhenAssigned_PreservesValue(string value)
    {
        var setting = new Setting { Value = value };
        Assert.Equal(value, setting.Value);
    }

    [Fact]
    public void Id_WhenAssigned_PreservesValue()
    {
        var setting = new Setting { Id = 99 };
        Assert.Equal(99, setting.Id);
    }

    [Fact]
    public void KeyAndValue_WhenAssignedTogether_PreserveBothValues()
    {
        var setting = new Setting { Key = "SomeKey", Value = "SomeValue" };

        Assert.Equal("SomeKey", setting.Key);
        Assert.Equal("SomeValue", setting.Value);
    }
}