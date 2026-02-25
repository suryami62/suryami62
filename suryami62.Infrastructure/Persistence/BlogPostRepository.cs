#region

using Microsoft.EntityFrameworkCore;
using suryami62.Application.Persistence;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Infrastructure.Persistence;

internal sealed class BlogPostRepository(ApplicationDbContext context) : IBlogPostRepository
{
    public async Task<(List<BlogPost> Items, int Total)> GetPostsAsync(
        bool onlyPublished = true,
        int? skip = null,
        int? take = null,
        string? searchTerm = null)
    {
        var query = context.BlogPosts.AsNoTracking().AsQueryable();
        if (onlyPublished) query = query.Where(p => p.IsPublished);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(p =>
                p.Title.Contains(searchTerm) || (p.Summary != null && p.Summary.Contains(searchTerm)));

        var total = await query.CountAsync().ConfigureAwait(false);
        var orderedQuery = query.OrderByDescending(p => p.Date);

        IQueryable<BlogPost> itemsQuery = orderedQuery;
        if (skip.HasValue) itemsQuery = itemsQuery.Skip(skip.Value);

        if (take.HasValue) itemsQuery = itemsQuery.Take(take.Value);

        var items = await itemsQuery.ToListAsync().ConfigureAwait(false);
        return (items, total);
    }

    public async Task<BlogPost?> GetBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(slug));

        return await context.BlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug).ConfigureAwait(false);
    }

    public async Task<BlogPost?> GetByIdAsync(int id)
    {
        return await context.BlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id).ConfigureAwait(false);
    }

    public async Task<BlogPost> CreateAsync(BlogPost post)
    {
        ArgumentNullException.ThrowIfNull(post);

        context.BlogPosts.Add(post);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return post;
    }

    public async Task UpdateAsync(BlogPost post)
    {
        ArgumentNullException.ThrowIfNull(post);

        var tracked = context.BlogPosts.Local.FirstOrDefault(p => p.Id == post.Id);
        if (tracked != null && !ReferenceEquals(tracked, post))
            context.Entry(tracked).CurrentValues.SetValues(post);
        else if (tracked == null) context.Entry(post).State = EntityState.Modified;

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

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