#region

using Microsoft.EntityFrameworkCore;
using suryami62.Application.Persistence;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Infrastructure.Persistence;

public sealed class BlogPostRepository : IBlogPostRepository
{
    private const string LikeEscapeCharacter = "\\";

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
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var filteredPosts = CreateFilteredPostsQuery(onlyPublished, searchTerm);

        var total = await filteredPosts.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await LoadPagedPostsAsync(filteredPosts, skip, take, cancellationToken).ConfigureAwait(false);

        return (items, total);
    }

    public async Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        EnsureSlug(slug);

        var post = await _context.BlogPosts
            .AsNoTracking()
            .FirstOrDefaultAsync(post => post.Slug == slug, cancellationToken)
            .ConfigureAwait(false);

        return post;
    }

    public async Task<BlogPost?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var post = await _context.BlogPosts
            .AsNoTracking()
            .FirstOrDefaultAsync(post => post.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return post;
    }

    public async Task<bool> SlugExistsAsync(
        string slug,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        var query = _context.BlogPosts
            .AsNoTracking()
            .Where(p => p.Slug == slug);

        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<BlogPost> CreateAsync(BlogPost post, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(post);

        _context.BlogPosts.Add(post);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return post;
    }

    public async Task UpdateAsync(BlogPost post, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(post);

        EfRepositoryHelpers.UpdateExistingOrAttachModified(
            _context,
            _context.BlogPosts,
            post,
            item => item.Id);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var post = await _context.BlogPosts
            .FindAsync(new object[] { id }, cancellationToken)
            .ConfigureAwait(false);

        if (post is null) return;

        _context.BlogPosts.Remove(post);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private IQueryable<BlogPost> CreateFilteredPostsQuery(bool onlyPublished, string? searchTerm)
    {
        var query = _context.BlogPosts.AsNoTracking();

        if (onlyPublished) query = query.Where(post => post.IsPublished);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchPattern = CreateContainsSearchPattern(searchTerm);

            query = query.Where(post =>
                EF.Functions.ILike(post.Title, searchPattern, LikeEscapeCharacter) ||
                EF.Functions.ILike(post.Summary, searchPattern, LikeEscapeCharacter));
        }

        return query;
    }

    private static string CreateContainsSearchPattern(string searchTerm)
    {
        var escapedTerm = searchTerm
            .Trim()
            .Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
            .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);

        return $"%{escapedTerm}%";
    }

    private static async Task<List<BlogPost>> LoadPagedPostsAsync(
        IQueryable<BlogPost> filteredPosts,
        int? skip,
        int? take,
        CancellationToken cancellationToken)
    {
        var sortedPosts = filteredPosts
            .OrderByDescending(post => post.Date);

        var pagedPosts = EfRepositoryHelpers
            .ApplyOptionalPaging(sortedPosts, skip, take);

        return await pagedPosts.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void EnsureSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug cannot be empty.", nameof(slug));
    }
}