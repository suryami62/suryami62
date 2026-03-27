#region

using suryami62.Domain.Models;

#endregion

namespace suryami62.Domain.Tests.Models;

public class JourneyHistoryTests
{
    [Fact]
    public void Constructor_WhenCreated_SetsExpectedDefaultValues()
    {
        var item = new JourneyHistory();

        Assert.Equal(string.Empty, item.Title);
        Assert.Equal(string.Empty, item.Organization);
        Assert.Equal(string.Empty, item.Period);
        Assert.Equal(string.Empty, item.Summary);
        Assert.Equal(JourneySection.None, item.Section);
    }

    [Theory]
    [InlineData(JourneySection.None)]
    [InlineData(JourneySection.Experience)]
    [InlineData(JourneySection.Education)]
    public void Section_WhenAssigned_PreservesValue(JourneySection section)
    {
        var item = new JourneyHistory { Section = section };
        Assert.Equal(section, item.Section);
    }

    [Theory]
    [InlineData("Software Engineer")]
    [InlineData("Senior Developer")]
    public void Title_WhenAssigned_PreservesValue(string title)
    {
        var item = new JourneyHistory { Title = title };
        Assert.Equal(title, item.Title);
    }

    [Fact]
    public void Organization_WhenAssigned_PreservesValue()
    {
        var item = new JourneyHistory { Organization = "Acme Corp" };
        Assert.Equal("Acme Corp", item.Organization);
    }

    [Fact]
    public void Period_WhenAssigned_PreservesValue()
    {
        var item = new JourneyHistory { Period = "2020 - 2023" };
        Assert.Equal("2020 - 2023", item.Period);
    }

    [Fact]
    public void Summary_WhenAssigned_PreservesValue()
    {
        const string summary = "Led development of core platform services.";
        var item = new JourneyHistory { Summary = summary };

        Assert.Equal(summary, item.Summary);
    }

    [Fact]
    public void Id_WhenAssigned_PreservesValue()
    {
        var item = new JourneyHistory { Id = 10 };
        Assert.Equal(10, item.Id);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void DisplayOrder_WhenAssigned_PreservesValue(int order)
    {
        var item = new JourneyHistory { DisplayOrder = order };
        Assert.Equal(order, item.DisplayOrder);
    }
}

public class JourneySectionTests
{
    [Fact]
    public void None_WhenCastToInt_ReturnsZero()
    {
        Assert.Equal(0, (int)JourneySection.None);
    }

    [Fact]
    public void Experience_WhenCastToInt_ReturnsOne()
    {
        Assert.Equal(1, (int)JourneySection.Experience);
    }

    [Fact]
    public void Education_WhenCastToInt_ReturnsTwo()
    {
        Assert.Equal(2, (int)JourneySection.Education);
    }

    [Fact]
    public void EnumValues_WhenEnumerated_AreDistinct()
    {
        var values = Enum.GetValues<JourneySection>();
        Assert.Equal(values.Length, values.Distinct().Count());
    }
}