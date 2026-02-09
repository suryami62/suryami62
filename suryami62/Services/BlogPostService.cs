#region

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using suryami62.Data;
using suryami62.Data.Models;

#endregion

namespace suryami62.Services;

internal interface IBlogPostService
{
    Task<List<BlogPost>> GetPostsAsync(bool onlyPublished = true);
    Task<BlogPost?> GetPostBySlugAsync(string slug);
    Task<BlogPost?> GetPostByIdAsync(int id);
    Task<BlogPost> CreatePostAsync(BlogPost post);
    Task UpdatePostAsync(BlogPost post);
    Task DeletePostAsync(int id);
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
internal sealed class BlogPostService : IBlogPostService
{
    private readonly ApplicationDbContext _context;

    public BlogPostService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BlogPost>> GetPostsAsync(bool onlyPublished = true)
    {
        var query = _context.BlogPosts.AsQueryable();
        if (onlyPublished) query = query.Where(p => p.IsPublished);
        return await query.OrderByDescending(p => p.Date).ToListAsync().ConfigureAwait(false);
    }

    public async Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        return await _context.BlogPosts.FirstOrDefaultAsync(p => p.Slug == slug).ConfigureAwait(false);
    }

    public async Task<BlogPost?> GetPostByIdAsync(int id)
    {
        return await _context.BlogPosts.FindAsync(id).ConfigureAwait(false);
    }

    public async Task<BlogPost> CreatePostAsync(BlogPost post)
    {
        _context.BlogPosts.Add(post);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return post;
    }

    public async Task UpdatePostAsync(BlogPost post)
    {
        var tracked = _context.BlogPosts.Local.FirstOrDefault(p => p.Id == post.Id);
        if (tracked != null && !ReferenceEquals(tracked, post))
            _context.Entry(tracked).CurrentValues.SetValues(post);
        else if (tracked == null) _context.Entry(post).State = EntityState.Modified;
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeletePostAsync(int id)
    {
        var post = await _context.BlogPosts.FindAsync(id).ConfigureAwait(false);
        if (post != null)
        {
            _context.BlogPosts.Remove(post);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}