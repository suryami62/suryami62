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
        return await EntityServiceHelper
            .CreateAsync(_context.BlogPosts, _context, post)
            .ConfigureAwait(false);
    }

    public async Task UpdatePostAsync(BlogPost post)
    {
        await EntityServiceHelper
            .UpdateAsync(_context.BlogPosts, _context, post, current => current.Id)
            .ConfigureAwait(false);
    }

    public async Task DeletePostAsync(int id)
    {
        await EntityServiceHelper
            .DeleteAsync(_context.BlogPosts, _context, id)
            .ConfigureAwait(false);
    }
}
