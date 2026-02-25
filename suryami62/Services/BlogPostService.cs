#region

using Microsoft.EntityFrameworkCore;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Services;

internal interface IBlogPostService
{
    Task<(List<BlogPost> Items, int Total)>
        GetPostsAsync(bool onlyPublished = true, int? skip = null, int? take = null, string? searchTerm = null);

    Task<BlogPost?> GetPostBySlugAsync(string slug);
    Task<BlogPost?> GetPostByIdAsync(int id);
    Task<BlogPost> CreatePostAsync(BlogPost post);
    Task UpdatePostAsync(BlogPost post);
    Task DeletePostAsync(int id);
}

internal sealed class BlogPostService(ApplicationDbContext context) : IBlogPostService
{
    public async Task<(List<BlogPost> Items, int Total)> GetPostsAsync(bool onlyPublished = true, int? skip = null,
        int? take = null, string? searchTerm = null)
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

    public async Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        return await context.BlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug).ConfigureAwait(false);
    }

    public async Task<BlogPost?> GetPostByIdAsync(int id)
    {
        return await context.BlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id).ConfigureAwait(false);
    }

    public async Task<BlogPost> CreatePostAsync(BlogPost post)
    {
        context.BlogPosts.Add(post);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return post;
    }

    public async Task UpdatePostAsync(BlogPost post)
    {
        var tracked = context.BlogPosts.Local.FirstOrDefault(p => p.Id == post.Id);
        if (tracked != null && !ReferenceEquals(tracked, post))
            context.Entry(tracked).CurrentValues.SetValues(post);
        else if (tracked == null) context.Entry(post).State = EntityState.Modified;
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeletePostAsync(int id)
    {
        var post = await context.BlogPosts.FindAsync(id).ConfigureAwait(false);
        if (post != null)
        {
            context.BlogPosts.Remove(post);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}