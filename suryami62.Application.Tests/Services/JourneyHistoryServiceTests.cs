#region

using Moq;
using suryami62.Application.Persistence;
using suryami62.Domain.Models;
using suryami62.Services;

#endregion

namespace suryami62.Application.Tests.Services;

public class JourneyHistoryServiceTests
{
    private readonly Mock<IJourneyHistoryRepository> _repositoryMock = new();
    private readonly JourneyHistoryService _service;

    public JourneyHistoryServiceTests()
    {
        _service = new JourneyHistoryService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetBySectionAsyncWithExperienceSectionDelegatesToRepository()
    {
        var items = new List<JourneyHistory>
        {
            new() { Title = "Software Engineer", Section = JourneySection.Experience }
        };
        _repositoryMock
            .Setup(r => r.GetBySectionAsync(JourneySection.Experience))
            .ReturnsAsync(items);

        var result = await _service.GetBySectionAsync(JourneySection.Experience);

        _repositoryMock.Verify(r => r.GetBySectionAsync(JourneySection.Experience), Times.Once);
        Assert.Single(result);
        Assert.Equal("Software Engineer", result[0].Title);
    }

    [Fact]
    public async Task GetBySectionAsyncWithEducationSectionDelegatesToRepository()
    {
        var items = new List<JourneyHistory>
        {
            new() { Title = "Computer Science", Section = JourneySection.Education }
        };
        _repositoryMock
            .Setup(r => r.GetBySectionAsync(JourneySection.Education))
            .ReturnsAsync(items);

        var result = await _service.GetBySectionAsync(JourneySection.Education);

        _repositoryMock.Verify(r => r.GetBySectionAsync(JourneySection.Education), Times.Once);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetBySectionAsyncWithEmptyResultReturnsEmptyList()
    {
        _repositoryMock
            .Setup(r => r.GetBySectionAsync(JourneySection.None))
            .ReturnsAsync([]);

        var result = await _service.GetBySectionAsync(JourneySection.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateAsyncWhenCalledDelegatesToRepositoryAndReturnsCreatedItem()
    {
        var item = new JourneyHistory { Title = "Developer", Section = JourneySection.Experience };
        _repositoryMock
            .Setup(r => r.CreateAsync(item))
            .ReturnsAsync(item);

        var result = await _service.CreateAsync(item);

        _repositoryMock.Verify(r => r.CreateAsync(item), Times.Once);
        Assert.Equal(item, result);
    }

    [Fact]
    public async Task DeleteAsyncWhenCalledDelegatesToRepository()
    {
        _repositoryMock
            .Setup(r => r.DeleteAsync(1))
            .Returns(Task.CompletedTask);

        await _service.DeleteAsync(1);

        _repositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }
}