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
        var postsQuery = context.BlogPosts.AsNoTracking();

        if (onlyPublished) postsQuery = postsQuery.Where(post => post.IsPublished);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            postsQuery = postsQuery.Where(post =>
                post.Title.Contains(searchTerm) || (post.Summary != null && post.Summary.Contains(searchTerm)));

        var total = await postsQuery.CountAsync().ConfigureAwait(false);

        postsQuery = postsQuery.OrderByDescending(post => post.Date);
        if (skip is > 0) postsQuery = postsQuery.Skip(skip.Value);

        if (take is > 0) postsQuery = postsQuery.Take(take.Value);

        var items = await postsQuery.ToListAsync().ConfigureAwait(false);
        return (items, total);
    }

    /// <inheritdoc />
    public async Task<BlogPost?> GetBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(slug));

        return await context.BlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BlogPost?> GetByIdAsync(int id)
    {
        return await context.BlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id).ConfigureAwait(false);
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

        var trackedPost = context.BlogPosts.Local.FirstOrDefault(p => p.Id == post.Id);
        if (trackedPost is not null && !ReferenceEquals(trackedPost, post))
            context.Entry(trackedPost).CurrentValues.SetValues(post);
        else if (trackedPost is null) context.Entry(post).State = EntityState.Modified;

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var post = await context.BlogPosts.FindAsync(id).ConfigureAwait(false);
        if (post != null)
        {
            context.BlogPosts.Remove(post);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}