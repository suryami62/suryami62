#region

using Moq;
using suryami62.Application.Persistence;
using suryami62.Domain.Models;
using suryami62.Services;

#endregion

namespace suryami62.Application.Tests.Services;

public class BlogPostServiceTests
{
    private readonly Mock<IBlogPostRepository> _repositoryMock = new();
    private readonly BlogPostService _service;

    public BlogPostServiceTests()
    {
        _service = new BlogPostService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetPostsAsyncWithDefaultsDelegatesToRepository()
    {
        var expected = (new List<BlogPost>(), 0);
        _repositoryMock
            .Setup(r => r.GetPostsAsync(true, null, null, null))
            .ReturnsAsync(expected);

        var result = await _service.GetPostsAsync();

        _repositoryMock.Verify(r => r.GetPostsAsync(true, null, null, null), Times.Once);
        Assert.Equal(expected.Item1, result.Items);
    }

    [Fact]
    public async Task GetPostsAsyncWithAllParametersPassesThemToRepository()
    {
        var posts = new List<BlogPost> { new() { Title = "Test" } };
        var expected = (posts, 1);
        _repositoryMock
            .Setup(r => r.GetPostsAsync(false, 10, 5, "dotnet"))
            .ReturnsAsync(expected);

        var result = await _service.GetPostsAsync(false, 10, 5, "dotnet");

        _repositoryMock.Verify(r => r.GetPostsAsync(false, 10, 5, "dotnet"), Times.Once);
        Assert.Equal(1, result.Total);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetPostBySlugAsyncWithExistingSlugReturnsPost()
    {
        var post = new BlogPost { Slug = "my-post", Title = "My Post" };
        _repositoryMock
            .Setup(r => r.GetBySlugAsync("my-post"))
            .ReturnsAsync(post);

        var result = await _service.GetPostBySlugAsync("my-post");

        _repositoryMock.Verify(r => r.GetBySlugAsync("my-post"), Times.Once);
        Assert.Equal(post, result);
    }

    [Fact]
    public async Task GetPostBySlugAsyncWithNonExistentSlugReturnsNull()
    {
        _repositoryMock
            .Setup(r => r.GetBySlugAsync("nonexistent"))
            .ReturnsAsync((BlogPost?)null);

        var result = await _service.GetPostBySlugAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPostByIdAsyncWithExistingIdReturnsPost()
    {
        var post = new BlogPost { Id = 1, Title = "Found Post" };
        _repositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(post);

        var result = await _service.GetPostByIdAsync(1);

        _repositoryMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        Assert.Equal(post, result);
    }

    [Fact]
    public async Task GetPostByIdAsyncWithNonExistentIdReturnsNull()
    {
        _repositoryMock
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((BlogPost?)null);

        var result = await _service.GetPostByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreatePostAsyncWhenCalledDelegatesToRepositoryAndReturnsCreatedPost()
    {
        var post = new BlogPost { Title = "New Post", Slug = "new-post" };
        _repositoryMock
            .Setup(r => r.CreateAsync(post))
            .ReturnsAsync(post);

        var result = await _service.CreatePostAsync(post);

        _repositoryMock.Verify(r => r.CreateAsync(post), Times.Once);
        Assert.Equal(post, result);
    }

    [Fact]
    public async Task UpdatePostAsyncWhenCalledDelegatesToRepository()
    {
        var post = new BlogPost { Id = 2, Title = "Updated Post" };
        _repositoryMock
            .Setup(r => r.UpdateAsync(post))
            .Returns(Task.CompletedTask);

        await _service.UpdatePostAsync(post);

        _repositoryMock.Verify(r => r.UpdateAsync(post), Times.Once);
    }

    [Fact]
    public async Task DeletePostAsyncWhenCalledDelegatesToRepository()
    {
        _repositoryMock
            .Setup(r => r.DeleteAsync(5))
            .Returns(Task.CompletedTask);

        await _service.DeletePostAsync(5);

        _repositoryMock.Verify(r => r.DeleteAsync(5), Times.Once);
    }
}