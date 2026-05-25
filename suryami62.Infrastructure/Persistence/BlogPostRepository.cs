#region

using Microsoft.EntityFrameworkCore;
using suryami62.Application.Persistence;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Infrastructure.Persistence;

public sealed class BlogPostRepository : IBlogPostRepository
{
    private readonly ApplicationDbContext _context;

    public BlogPostRepository(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

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

    public async Task<BlogPost?> GetBySlugAsync(string slug)
    {
        EnsureSlug(slug);

        var post = await _context.BlogPosts
            .AsNoTracking()
            .FirstOrDefaultAsync(post => post.Slug == slug)
            .ConfigureAwait(false);

        return post;
    }

    public async Task<BlogPost?> GetByIdAsync(int id)
    {
        var post = await _context.BlogPosts
            .AsNoTracking()
            .FirstOrDefaultAsync(post => post.Id == id)
            .ConfigureAwait(false);

        return post;
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        var query = _context.BlogPosts
            .AsNoTracking()
            .Where(p => p.Slug == slug);

        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);

        return await query.AnyAsync().ConfigureAwait(false);
    }

    public async Task<BlogPost> CreateAsync(BlogPost post)
    {
        ArgumentNullException.ThrowIfNull(post);

        _context.BlogPosts.Add(post);

        await _context.SaveChangesAsync().ConfigureAwait(false);

        return post;
    }

    public async Task UpdateAsync(BlogPost post)
    {
        ArgumentNullException.ThrowIfNull(post);

        EfRepositoryHelpers.UpdateExistingOrAttachModified(
            _context,
            _context.BlogPosts,
            post,
            item => item.Id);

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id)
    {
        var post = await _context.BlogPosts
            .FindAsync(id)
            .ConfigureAwait(false);

        if (post is null) return;

        _context.BlogPosts.Remove(post);

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    private IQueryable<BlogPost> CreateFilteredPostsQuery(bool onlyPublished, string? searchTerm)
    {
        var query = _context.BlogPosts.AsNoTracking();

        if (onlyPublished) query = query.Where(post => post.IsPublished);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(post =>
                post.Title.Contains(searchTerm) ||
                post.Summary.Contains(searchTerm));

        return query;
    }

    private static async Task<List<BlogPost>> LoadPagedPostsAsync(
        IQueryable<BlogPost> filteredPosts,
        int? skip,
        int? take)
    {
        var sortedPosts = filteredPosts
            .OrderByDescending(post => post.Date);

        var pagedPosts = EfRepositoryHelpers
            .ApplyOptionalPaging(sortedPosts, skip, take);

        return await pagedPosts.ToListAsync().ConfigureAwait(false);
    }

    private static void EnsureSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug cannot be empty.", nameof(slug));
    }
}