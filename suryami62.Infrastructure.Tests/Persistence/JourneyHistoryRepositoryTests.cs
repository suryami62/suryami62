#region

using suryami62.Domain.Models;
using suryami62.Infrastructure.Persistence;

#endregion

namespace suryami62.Infrastructure.Tests.Persistence;

public class JourneyHistoryRepositoryTests
{
    private static JourneyHistory CreateItem(
        string title = "Software Engineer",
        string organization = "Company",
        JourneySection section = JourneySection.Experience)
    {
        return new JourneyHistory
        {
            Title = title,
            Organization = organization,
            Period = "2020-2023",
            Summary = "Led platform development.",
            Section = section
        };
    }

    [Fact]
    public async Task GetBySectionAsyncWithExperienceSectionReturnsOnlyExperienceItems()
    {
        await using var context = DbContextFactory.CreateInMemory();
        context.JourneyHistories.AddRange(
            CreateItem("Engineer", section: JourneySection.Experience),
            CreateItem("CS Degree", section: JourneySection.Education));
        await context.SaveChangesAsync();

        var repository = new JourneyHistoryRepository(context);
        var result = await repository.GetBySectionAsync(JourneySection.Experience);

        Assert.Single(result);
        Assert.Equal("Engineer", result[0].Title);
    }

    [Fact]
    public async Task GetBySectionAsyncWithEducationSectionReturnsOnlyEducationItems()
    {
        await using var context = DbContextFactory.CreateInMemory();
        context.JourneyHistories.AddRange(
            CreateItem("Engineer", section: JourneySection.Experience),
            CreateItem("CS Degree", section: JourneySection.Education));
        await context.SaveChangesAsync();

        var repository = new JourneyHistoryRepository(context);
        var result = await repository.GetBySectionAsync(JourneySection.Education);

        Assert.Single(result);
        Assert.Equal("CS Degree", result[0].Title);
    }

    [Fact]
    public async Task GetBySectionAsyncWithNoMatchingItemsReturnsEmptyList()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new JourneyHistoryRepository(context);

        var result = await repository.GetBySectionAsync(JourneySection.Experience);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBySectionAsyncReturnsItemsOrderedByDisplayOrder()
    {
        await using var context = DbContextFactory.CreateInMemory();
        context.JourneyHistories.AddRange(
            new JourneyHistory
            {
                Title = "Third", Organization = "O", Period = "P", Section = JourneySection.Experience, DisplayOrder = 3
            },
            new JourneyHistory
            {
                Title = "First", Organization = "O", Period = "P", Section = JourneySection.Experience, DisplayOrder = 1
            },
            new JourneyHistory
            {
                Title = "Second", Organization = "O", Period = "P", Section = JourneySection.Experience,
                DisplayOrder = 2
            });
        await context.SaveChangesAsync();

        var repository = new JourneyHistoryRepository(context);
        var result = await repository.GetBySectionAsync(JourneySection.Experience);

        Assert.Equal("First", result[0].Title);
        Assert.Equal("Second", result[1].Title);
        Assert.Equal("Third", result[2].Title);
    }

    [Fact]
    public async Task CreateAsyncWithFirstItemSetsDisplayOrderToOne()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new JourneyHistoryRepository(context);
        var item = CreateItem();

        var created = await repository.CreateAsync(item);

        Assert.Equal(1, created.DisplayOrder);
    }

    [Fact]
    public async Task CreateAsyncWithExistingItemsIncrementsDisplayOrder()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var item1 = CreateItem("First");
        context.JourneyHistories.Add(item1);
        item1.DisplayOrder = 1;
        await context.SaveChangesAsync();

        var repository = new JourneyHistoryRepository(context);
        var item2 = CreateItem("Second");
        var created = await repository.CreateAsync(item2);

        Assert.Equal(2, created.DisplayOrder);
    }

    [Fact]
    public async Task CreateAsyncWithNullItemThrowsArgumentNullException()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new JourneyHistoryRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.CreateAsync(null!));
    }

    [Fact]
    public async Task DeleteAsyncWithExistingIdRemovesItem()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var item = CreateItem();
        context.JourneyHistories.Add(item);
        await context.SaveChangesAsync();

        var repository = new JourneyHistoryRepository(context);
        await repository.DeleteAsync(item.Id);

        Assert.Empty(context.JourneyHistories.ToList());
    }

    [Fact]
    public async Task DeleteAsyncWithNonExistentIdDoesNotThrow()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new JourneyHistoryRepository(context);

        var exception = await Record.ExceptionAsync(() => repository.DeleteAsync(9999));

        Assert.Null(exception);
    }
}