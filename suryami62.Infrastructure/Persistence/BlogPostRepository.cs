#region

using Microsoft.EntityFrameworkCore;
using suryami62.Application.Persistence;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Infrastructure.Persistence;

/// <summary>
///     Persists and queries blog posts using Entity Framework Core.
/// </summary>
public sealed class BlogPostRepository(ApplicationDbContext context) : IBlogPostRepository
{
    /// <inheritdoc />
    public async Task<(List<BlogPost> Items, int Total)> GetPostsAsync(
        bool onlyPublished = true,
        int? skip = null,
        int? take = null,
        string? searchTerm = null)
    {
        var filteredPosts = CreateFilteredPostsQuery(onlyPublished, searchTerm);
        var total = await filteredPosts.CountAsync().ConfigureAwait(false);
        var items = await LoadPagedPostsAsync(filteredPosts, skip, take).ConfigureAwait(false);

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<BlogPost?> GetBySlugAsync(string slug)
    {
        EnsureSlug(slug);

        return await context.BlogPosts
            .AsNoTracking()
            .FirstOrDefaultAsync(post => post.Slug == slug)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BlogPost?> GetByIdAsync(int id)
    {
        return await context.BlogPosts
            .AsNoTracking()
            .FirstOrDefaultAsync(post => post.Id == id)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BlogPost> CreateAsync(BlogPost post)
    {
        ArgumentNullException.ThrowIfNull(post);

        context.BlogPosts.Add(post);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return post;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(BlogPost post)
    {
        ArgumentNullException.ThrowIfNull(post);

        EfRepositoryHelpers.UpdateExistingOrAttachModified(context, context.BlogPosts, post, item => item.Id);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var post = await context.BlogPosts.FindAsync(id).ConfigureAwait(false);
        if (post is null) return;

        context.BlogPosts.Remove(post);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    private IQueryable<BlogPost> CreateFilteredPostsQuery(bool onlyPublished, string? searchTerm)
    {
        var query = context.BlogPosts.AsNoTracking();

        if (onlyPublished) query = query.Where(post => post.IsPublished);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(post => post.Title.Contains(searchTerm) || post.Summary.Contains(searchTerm));

        return query;
    }

    private static Task<List<BlogPost>> LoadPagedPostsAsync(
        IQueryable<BlogPost> filteredPosts,
        int? skip,
        int? take)
    {
        var sortedPosts = filteredPosts.OrderByDescending(post => post.Date);
        var pagedPosts = EfRepositoryHelpers.ApplyOptionalPaging(sortedPosts, skip, take);
        return pagedPosts.ToListAsync();
    }

    private static void EnsureSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug cannot be empty.", nameof(slug));
    }
}