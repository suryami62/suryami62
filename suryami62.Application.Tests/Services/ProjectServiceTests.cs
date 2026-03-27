#region

using Moq;
using suryami62.Application.Persistence;
using suryami62.Domain.Models;
using suryami62.Services;

#endregion

namespace suryami62.Application.Tests.Services;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _repositoryMock = new();
    private readonly ProjectService _service;

    public ProjectServiceTests()
    {
        _service = new ProjectService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetProjectsAsyncWithDefaultsDelegatesToRepository()
    {
        var expected = (new List<Project>(), 0);
        _repositoryMock
            .Setup(r => r.GetProjectsAsync(null, null))
            .ReturnsAsync(expected);

        var result = await _service.GetProjectsAsync();

        _repositoryMock.Verify(r => r.GetProjectsAsync(null, null), Times.Once);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetProjectsAsyncWithPagingPassesParametersToRepository()
    {
        var projects = new List<Project> { new() { Title = "My Project" } };
        var expected = (projects, 1);
        _repositoryMock
            .Setup(r => r.GetProjectsAsync(0, 10))
            .ReturnsAsync(expected);

        var result = await _service.GetProjectsAsync(0, 10);

        _repositoryMock.Verify(r => r.GetProjectsAsync(0, 10), Times.Once);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetProjectByIdAsyncWithExistingIdReturnsProject()
    {
        var project = new Project { Id = 3, Title = "Found Project" };
        _repositoryMock
            .Setup(r => r.GetByIdAsync(3))
            .ReturnsAsync(project);

        var result = await _service.GetProjectByIdAsync(3);

        _repositoryMock.Verify(r => r.GetByIdAsync(3), Times.Once);
        Assert.Equal(project, result);
    }

    [Fact]
    public async Task GetProjectByIdAsyncWithNonExistentIdReturnsNull()
    {
        _repositoryMock
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Project?)null);

        var result = await _service.GetProjectByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateProjectAsyncWhenCalledDelegatesToRepositoryAndReturnsCreatedProject()
    {
        var project = new Project { Title = "New Project" };
        _repositoryMock
            .Setup(r => r.CreateAsync(project))
            .ReturnsAsync(project);

        var result = await _service.CreateProjectAsync(project);

        _repositoryMock.Verify(r => r.CreateAsync(project), Times.Once);
        Assert.Equal(project, result);
    }

    [Fact]
    public async Task UpdateProjectAsyncWhenCalledDelegatesToRepository()
    {
        var project = new Project { Id = 1, Title = "Updated Project" };
        _repositoryMock
            .Setup(r => r.UpdateAsync(project))
            .Returns(Task.CompletedTask);

        await _service.UpdateProjectAsync(project);

        _repositoryMock.Verify(r => r.UpdateAsync(project), Times.Once);
    }

    [Fact]
    public async Task DeleteProjectAsyncWhenCalledDelegatesToRepository()
    {
        _repositoryMock
            .Setup(r => r.DeleteAsync(4))
            .Returns(Task.CompletedTask);

        await _service.DeleteProjectAsync(4);

        _repositoryMock.Verify(r => r.DeleteAsync(4), Times.Once);
    }
}