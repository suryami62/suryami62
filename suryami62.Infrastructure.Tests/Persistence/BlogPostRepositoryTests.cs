#region

using suryami62.Domain.Models;
using suryami62.Infrastructure.Persistence;

#endregion

namespace suryami62.Infrastructure.Tests.Persistence;

public class BlogPostRepositoryTests
{
    private static BlogPost CreatePost(
        string title = "Test Post",
        string slug = "test-post",
        bool isPublished = true,
        string summary = "A summary")
    {
        return new BlogPost
        {
            Title = title,
            Slug = slug,
            Content = "Some content",
            Label = "label",
            Summary = summary,
            IsPublished = isPublished,
            Date = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetPostsAsyncWithOnlyPublishedTrueReturnsOnlyPublishedPosts()
    {
        await using var context = DbContextFactory.CreateInMemory();
        context.BlogPosts.AddRange(
            CreatePost("Published", "published"),
            CreatePost("Draft", "draft", false));
        await context.SaveChangesAsync();

        var repository = new BlogPostRepository(context);
        var (items, total) = await repository.GetPostsAsync();

        Assert.Single(items);
        Assert.Equal(1, total);
        Assert.Equal("Published", items[0].Title);
    }

    [Fact]
    public async Task GetPostsAsyncWithOnlyPublishedFalseReturnsAllPosts()
    {
        await using var context = DbContextFactory.CreateInMemory();
        context.BlogPosts.AddRange(
            CreatePost("Published", "published"),
            CreatePost("Draft", "draft", false));
        await context.SaveChangesAsync();

        var repository = new BlogPostRepository(context);
        var (items, total) = await repository.GetPostsAsync(false);

        Assert.Equal(2, items.Count);
        Assert.Equal(2, total);
    }

    [Fact]
    public async Task GetPostsAsyncWithMatchingSearchTermFiltersResults()
    {
        await using var context = DbContextFactory.CreateInMemory();
        context.BlogPosts.AddRange(
            new BlogPost
            {
                Title = "C# Tips", Slug = "cs-tips", Content = "x", Label = "dev", Summary = "C# tricks",
                IsPublished = true
            },
            new BlogPost
            {
                Title = "Python Guide", Slug = "python", Content = "x", Label = "dev", Summary = "Basic Python",
                IsPublished = true
            });
        await context.SaveChangesAsync();

        var repository = new BlogPostRepository(context);
        var (items, _) = await repository.GetPostsAsync(searchTerm: "C#");

        Assert.Single(items);
        Assert.Equal("C# Tips", items[0].Title);
    }

    [Fact]
    public async Task GetPostsAsyncSearchTermInSummaryFiltersResults()
    {
        await using var context = DbContextFactory.CreateInMemory();
        context.BlogPosts.AddRange(
            new BlogPost
            {
                Title = "Post A", Slug = "post-a", Content = "x", Label = "dev", Summary = "blazor tips here",
                IsPublished = true
            },
            new BlogPost
            {
                Title = "Post B", Slug = "post-b", Content = "x", Label = "dev", Summary = "something else",
                IsPublished = true
            });
        await context.SaveChangesAsync();

        var repository = new BlogPostRepository(context);
        var (items, _) = await repository.GetPostsAsync(searchTerm: "blazor");

        Assert.Single(items);
        Assert.Equal("Post A", items[0].Title);
    }

    [Fact]
    public async Task GetPostsAsyncWithPagingReturnsCorrectTotalAndSubset()
    {
        await using var context = DbContextFactory.CreateInMemory();
        for (var i = 1; i <= 5; i++) context.BlogPosts.Add(CreatePost($"Post {i}", $"post-{i}"));
        await context.SaveChangesAsync();

        var repository = new BlogPostRepository(context);
        var (items, total) = await repository.GetPostsAsync(skip: 2, take: 2);

        Assert.Equal(2, items.Count);
        Assert.Equal(5, total);
    }

    [Fact]
    public async Task GetBySlugAsyncWithExistingSlugReturnsPost()
    {
        await using var context = DbContextFactory.CreateInMemory();
        context.BlogPosts.Add(CreatePost("My Post", "my-post"));
        await context.SaveChangesAsync();

        var repository = new BlogPostRepository(context);
        var result = await repository.GetBySlugAsync("my-post");

        Assert.NotNull(result);
        Assert.Equal("my-post", result.Slug);
    }

    [Fact]
    public async Task GetBySlugAsyncWithNonExistentSlugReturnsNull()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new BlogPostRepository(context);

        var result = await repository.GetBySlugAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySlugAsyncWithEmptySlugThrowsArgumentException()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new BlogPostRepository(context);

        await Assert.ThrowsAsync<ArgumentException>(() => repository.GetBySlugAsync(string.Empty));
    }

    [Fact]
    public async Task GetByIdAsyncWithExistingIdReturnsPost()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var post = CreatePost("My Post", "my-post");
        context.BlogPosts.Add(post);
        await context.SaveChangesAsync();

        var repository = new BlogPostRepository(context);
        var result = await repository.GetByIdAsync(post.Id);

        Assert.NotNull(result);
        Assert.Equal(post.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsyncWithInvalidIdReturnsNull()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new BlogPostRepository(context);

        var result = await repository.GetByIdAsync(9999);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsyncWithValidPostPersistsPost()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new BlogPostRepository(context);
        var post = CreatePost("New Post", "new-post");

        var created = await repository.CreateAsync(post);

        Assert.NotEqual(0, created.Id);
        Assert.Equal("New Post", created.Title);
        Assert.Single(context.BlogPosts.ToList());
    }

    [Fact]
    public async Task CreateAsyncWithNullPostThrowsArgumentNullException()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new BlogPostRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.CreateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsyncWithExistingPostUpdatesValues()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var post = CreatePost("Original Title", "original");
        context.BlogPosts.Add(post);
        await context.SaveChangesAsync();

        var repository = new BlogPostRepository(context);
        post.Title = "Updated Title";
        await repository.UpdateAsync(post);

        var updated = await context.BlogPosts.FindAsync(post.Id);
        Assert.Equal("Updated Title", updated!.Title);
    }

    [Fact]
    public async Task UpdateAsyncWithNullPostThrowsArgumentNullException()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new BlogPostRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task DeleteAsyncWithExistingPostRemovesPost()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var post = CreatePost("Delete Me", "delete-me");
        context.BlogPosts.Add(post);
        await context.SaveChangesAsync();

        var repository = new BlogPostRepository(context);
        await repository.DeleteAsync(post.Id);

        Assert.Empty(context.BlogPosts.ToList());
    }

    [Fact]
    public async Task DeleteAsyncWithNonExistentIdDoesNotThrow()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new BlogPostRepository(context);

        var exception = await Record.ExceptionAsync(() => repository.DeleteAsync(9999));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetPostsAsyncWithEmptySearchTermReturnsAllPublishedPosts()
    {
        await using var context = DbContextFactory.CreateInMemory();
        context.BlogPosts.AddRange(
            CreatePost("Post 1", "post-1"),
            CreatePost("Post 2", "post-2"));
        await context.SaveChangesAsync();

        var repository = new BlogPostRepository(context);
        var (items, total) = await repository.GetPostsAsync(searchTerm: "");

        Assert.Equal(2, items.Count);
        Assert.Equal(2, total);
    }

    [Fact]
    public async Task GetPostsAsyncSearchTermIsCaseSensitive()
    {
        await using var context = DbContextFactory.CreateInMemory();
        context.BlogPosts.Add(new BlogPost
        {
            Title = "DotNet Core", Slug = "dotnet", Content = "x", Label = "dev",
            Summary = "Learn DOTNET", IsPublished = true
        });
        await context.SaveChangesAsync();

        var repository = new BlogPostRepository(context);
        var (items, _) = await repository.GetPostsAsync(searchTerm: "dotnet");

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetPostsAsyncWithSkipBeyondTotalReturnsEmptyList()
    {
        await using var context = DbContextFactory.CreateInMemory();
        context.BlogPosts.Add(CreatePost("Post 1", "post-1"));
        await context.SaveChangesAsync();

        var repository = new BlogPostRepository(context);
        var (items, total) = await repository.GetPostsAsync(skip: 100, take: 10);

        Assert.Empty(items);
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task CreateAsyncWithUnpublishedPostPersistsCorrectly()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new BlogPostRepository(context);
        var post = CreatePost("Draft Post", "draft-post", false);

        var created = await repository.CreateAsync(post);

        Assert.NotEqual(0, created.Id);
        Assert.False(created.IsPublished);
    }

    [Fact]
    public async Task SlugExistsAsyncWithExistingSlugReturnsTrue()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var post = CreatePost("My Post", "my-post");
        context.BlogPosts.Add(post);
        await context.SaveChangesAsync();

        var repository = new BlogPostRepository(context);
        var result = await repository.SlugExistsAsync("my-post");

        Assert.True(result);
    }

    [Fact]
    public async Task SlugExistsAsyncWithNonExistingSlugReturnsFalse()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new BlogPostRepository(context);

        var result = await repository.SlugExistsAsync("nonexistent");

        Assert.False(result);
    }

    [Fact]
    public async Task SlugExistsAsyncWithExcludedIdIgnoresThatPost()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var post = CreatePost("My Post", "my-post");
        context.BlogPosts.Add(post);
        await context.SaveChangesAsync();

        var repository = new BlogPostRepository(context);
        var result = await repository.SlugExistsAsync("my-post", post.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task SlugExistsAsyncWithEmptySlugReturnsFalse()
    {
        await using var context = DbContextFactory.CreateInMemory();
        var repository = new BlogPostRepository(context);

        var result = await repository.SlugExistsAsync(string.Empty);

        Assert.False(result);
    }
}