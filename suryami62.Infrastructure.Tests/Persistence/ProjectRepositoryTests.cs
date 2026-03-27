#region

using suryami62.Domain.Models;
using suryami62.Infrastructure.Persistence;

#endregion

namespace suryami62.Infrastructure.Tests.Persistence;

public class ProjectRepositoryTests
{
    private static Project CreateProject(string title = "My Project", int displayOrder = 0)
    {
        return new Project
        {
            Title = title,
            Description = "A portfolio project.",
            Tags = "C#,.NET",
            DisplayOrder = displayOrder
        };
    }

    [Fact]
    public async Task GetProjectsAsyncWithNoProjectsReturnsEmptyResult()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new ProjectRepository(context);

        var (items, total) = await repository.GetProjectsAsync();

        Assert.Empty(items);
        Assert.Equal(0, total);
    }

    [Fact]
    public async Task GetProjectsAsyncWithProjectsReturnsAllProjects()
    {
        await using var context = DbContextFactory.CreateInMemory();
        context.Projects.AddRange(CreateProject("Alpha", 1), CreateProject("Beta", 2));
        await context.SaveChangesAsync();

        var repository = new ProjectRepository(context);
        var (items, total) = await repository.GetProjectsAsync();

        Assert.Equal(2, items.Count);
        Assert.Equal(2, total);
    }

    [Fact]
    public async Task GetProjectsAsyncReturnsProjectsOrderedByDisplayOrder()
    {
        await using var context = DbContextFactory.CreateInMemory();
        context.Projects.AddRange(CreateProject("Third", 3), CreateProject("First", 1), CreateProject("Second", 2));
        await context.SaveChangesAsync();

        var repository = new ProjectRepository(context);
        var (items, _) = await repository.GetProjectsAsync();

        Assert.Equal("First", items[0].Title);
        Assert.Equal("Second", items[1].Title);
        Assert.Equal("Third", items[2].Title);
    }

    [Fact]
    public async Task GetProjectsAsyncWithSkipReturnsCorrectSubset()
    {
        await using var context = DbContextFactory.CreateInMemory();
        for (var i = 1; i <= 5; i++) context.Projects.Add(CreateProject($"Project {i}", i));
        await context.SaveChangesAsync();

        var repository = new ProjectRepository(context);
        var (items, total) = await repository.GetProjectsAsync(3);

        Assert.Equal(2, items.Count);
        Assert.Equal(5, total);
    }

    [Fact]
    public async Task GetProjectsAsyncWithTakeReturnsCorrectSubset()
    {
        await using var context = DbContextFactory.CreateInMemory();
        for (var i = 1; i <= 5; i++) context.Projects.Add(CreateProject($"Project {i}", i));
        await context.SaveChangesAsync();

        var repository = new ProjectRepository(context);
        var (items, total) = await repository.GetProjectsAsync(take: 2);

        Assert.Equal(2, items.Count);
        Assert.Equal(5, total);
    }

    [Fact]
    public async Task GetByIdAsyncWithExistingIdReturnsProject()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var project = CreateProject("Found Project");
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var repository = new ProjectRepository(context);
        var result = await repository.GetByIdAsync(project.Id);

        Assert.NotNull(result);
        Assert.Equal(project.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsyncWithNonExistentIdReturnsNull()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new ProjectRepository(context);

        var result = await repository.GetByIdAsync(9999);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsyncWithValidProjectPersistsProject()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new ProjectRepository(context);
        var project = CreateProject("New Project");

        var created = await repository.CreateAsync(project);

        Assert.NotEqual(0, created.Id);
        Assert.Equal("New Project", created.Title);
        Assert.Single(context.Projects.ToList());
    }

    [Fact]
    public async Task CreateAsyncWithNullProjectThrowsArgumentNullException()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new ProjectRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.CreateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsyncWithExistingProjectUpdatesValues()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var project = CreateProject("Original");
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var repository = new ProjectRepository(context);
        project.Title = "Updated Title";
        await repository.UpdateAsync(project);

        var updated = await context.Projects.FindAsync(project.Id);
        Assert.Equal("Updated Title", updated!.Title);
    }

    [Fact]
    public async Task UpdateAsyncWithNullProjectThrowsArgumentNullException()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new ProjectRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task DeleteAsyncWithExistingProjectRemovesProject()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var project = CreateProject("Delete Me");
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var repository = new ProjectRepository(context);
        await repository.DeleteAsync(project.Id);

        Assert.Empty(context.Projects.ToList());
    }

    [Fact]
    public async Task DeleteAsyncWithNonExistentIdDoesNotThrow()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new ProjectRepository(context);

        var exception = await Record.ExceptionAsync(() => repository.DeleteAsync(9999));

        Assert.Null(exception);
    }
}