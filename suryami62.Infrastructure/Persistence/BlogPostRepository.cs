// ============================================================================
// BLOG POST REPOSITORY
// ============================================================================
// This class implements the IBlogPostRepository interface using Entity Framework Core.
// It performs actual database operations (SQL queries) to store and retrieve blog posts.
//
// WHAT IS ENTITY FRAMEWORK CORE (EF Core)?
// EF Core is an "Object-Relational Mapper" (ORM). It translates between:
// - C# objects (BlogPost class) and
// - Database tables/rows (SQL)
//
// You write C# code like "context.BlogPosts.Where(...)", EF Core generates SQL like:
//   SELECT * FROM BlogPosts WHERE IsPublished = 1
//
// KEY CONCEPTS:
// - DbContext (context): Your database connection + all tables
// - DbSet (context.BlogPosts): Represents one table (BlogPosts table)
// - IQueryable: A query that hasn't executed yet (waiting for ToListAsync, CountAsync, etc.)
// - AsNoTracking(): Read-only query (faster, no change tracking)
//
// ASNOTRACKING() EXPLAINED:
// EF Core normally tracks changes to entities so it can save them later.
// For read-only queries (list views), we use AsNoTracking() to skip this.
// Result: Faster queries, less memory usage.
//
// SAVECHANGESASYNC() EXPLAINED:
// Changes to entities (Add, Update, Remove) are queued in memory.
// SaveChangesAsync() applies all queued changes to the database in one transaction.
// ============================================================================

#region

using Microsoft.EntityFrameworkCore;
using suryami62.Application.Persistence;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Infrastructure.Persistence;

/// <summary>
///     Implements blog post data access using Entity Framework Core.
///     Handles database queries and operations for the BlogPosts table.
/// </summary>
public sealed class BlogPostRepository : IBlogPostRepository
{
    // The database context - provides access to all tables
    private readonly ApplicationDbContext _context;

    /// <summary>
    ///     Creates a new blog post repository with the given database context.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    public BlogPostRepository(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <summary>
    ///     Gets a paginated list of blog posts with optional filtering.
    /// </summary>
    /// <param name="onlyPublished">If true, only return published posts.</param>
    /// <param name="skip">Number of posts to skip (pagination).</param>
    /// <param name="take">Maximum posts to return (page size).</param>
    /// <param name="searchTerm">Optional text to search in titles/summaries.</param>
    /// <returns>Tuple with (list of posts, total matching count).</returns>
    public async Task<(List<BlogPost> Items, int Total)> GetPostsAsync(
        bool onlyPublished = true,
        int? skip = null,
        int? take = null,
        string? searchTerm = null)
    {
        // Step 1: Build the filtered query (not executed yet)
        var filteredPosts = CreateFilteredPostsQuery(onlyPublished, searchTerm);

        // Step 2: Count total matching posts (for pagination "Showing X of Y")
        // This executes a SQL COUNT query
        var total = await filteredPosts.CountAsync().ConfigureAwait(false);

        // Step 3: Load the actual posts for this page
        // This executes a SQL SELECT with LIMIT/OFFSET (skip/take)
        var items = await LoadPagedPostsAsync(filteredPosts, skip, take).ConfigureAwait(false);

        // Step 4: Return both the items and total count
        return (items, total);
    }

    /// <summary>
    ///     Gets a single blog post by its URL slug.
    ///     Used when visitor navigates to /posts/my-post-slug.
    /// </summary>
    /// <param name="slug">The URL-friendly post identifier.</param>
    /// <returns>The blog post, or null if not found.</returns>
    public async Task<BlogPost?> GetBySlugAsync(string slug)
    {
        // Step 1: Validate input
        EnsureSlug(slug);

        // Step 2: Query for post with matching slug
        // AsNoTracking() because this is a read-only query
        var post = await _context.BlogPosts
            .AsNoTracking()
            .FirstOrDefaultAsync(post => post.Slug == slug)
            .ConfigureAwait(false);

        return post;
    }

    /// <summary>
    ///     Gets a single blog post by its database ID.
    ///     Used in admin area for editing existing posts.
    /// </summary>
    /// <param name="id">The numeric database ID.</param>
    /// <returns>The blog post, or null if not found.</returns>
    public async Task<BlogPost?> GetByIdAsync(int id)
    {
        // Query for post with matching ID
        // AsNoTracking() because this is a read-only query
        var post = await _context.BlogPosts
            .AsNoTracking()
            .FirstOrDefaultAsync(post => post.Id == id)
            .ConfigureAwait(false);

        return post;
    }

    /// <summary>
    ///     Checks if a slug already exists in the database.
    ///     Used to prevent duplicate slugs when creating or updating posts.
    /// </summary>
    /// <param name="slug">The slug to check.</param>
    /// <param name="excludeId">Optional post ID to exclude from check (for updates).</param>
    /// <returns>True if slug exists; otherwise false.</returns>
    public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        // Build query: check if any post has this slug
        var query = _context.BlogPosts
            .AsNoTracking()
            .Where(p => p.Slug == slug);

        // If updating, exclude the current post from the check
        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);

        // Execute: check if any matching post exists
        return await query.AnyAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Creates a new blog post in the database.
    /// </summary>
    /// <param name="post">The blog post to create.</param>
    /// <returns>The created post (with assigned ID).</returns>
    public async Task<BlogPost> CreateAsync(BlogPost post)
    {
        // Step 1: Validate input
        ArgumentNullException.ThrowIfNull(post);

        // Step 2: Mark post for insertion
        // This adds the post to EF Core's change tracker (not database yet)
        _context.BlogPosts.Add(post);

        // Step 3: Save changes to database
        // This executes INSERT SQL statement
        await _context.SaveChangesAsync().ConfigureAwait(false);

        // Step 4: Return the post (now with Id populated by database)
        return post;
    }

    /// <summary>
    ///     Updates an existing blog post in the database.
    /// </summary>
    /// <param name="post">The post with updated values.</param>
    public async Task UpdateAsync(BlogPost post)
    {
        // Step 1: Validate input
        ArgumentNullException.ThrowIfNull(post);

        // Step 2: Attach post to context and mark as modified
        // EfRepositoryHelpers handles complex attach/update logic
        EfRepositoryHelpers.UpdateExistingOrAttachModified(
            _context,
            _context.BlogPosts,
            post,
            item => item.Id);

        // Step 3: Save changes to database
        // This executes UPDATE SQL statement
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Deletes a blog post from the database.
    /// </summary>
    /// <param name="id">The ID of the post to delete.</param>
    public async Task DeleteAsync(int id)
    {
        // Step 1: Find the post by ID
        // FindAsync is efficient - checks memory cache first, then database
        var post = await _context.BlogPosts
            .FindAsync(id)
            .ConfigureAwait(false);

        // Step 2: If post not found, nothing to delete
        if (post is null) return;

        // Step 3: Mark post for deletion
        _context.BlogPosts.Remove(post);

        // Step 4: Save changes to database
        // This executes DELETE SQL statement
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Builds a query for blog posts with optional filtering.
    ///     Returns IQueryable (query not executed yet - just built).
    /// </summary>
    private IQueryable<BlogPost> CreateFilteredPostsQuery(bool onlyPublished, string? searchTerm)
    {
        // Start with all blog posts, no tracking (read-only)
        var query = _context.BlogPosts.AsNoTracking();

        // Filter 1: Only published posts (if requested)
        if (onlyPublished) query = query.Where(post => post.IsPublished);

        // Filter 2: Search term in title or summary
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(post =>
                post.Title.Contains(searchTerm) ||
                post.Summary.Contains(searchTerm));

        return query;
    }

    /// <summary>
    ///     Applies sorting and pagination to a query, then executes it.
    /// </summary>
    private static async Task<List<BlogPost>> LoadPagedPostsAsync(
        IQueryable<BlogPost> filteredPosts,
        int? skip,
        int? take)
    {
        // Step 1: Sort by date (newest first)
        var sortedPosts = filteredPosts
            .OrderByDescending(post => post.Date);

        // Step 2: Apply pagination (skip/take)
        var pagedPosts = EfRepositoryHelpers
            .ApplyOptionalPaging(sortedPosts, skip, take);

        // Step 3: Execute query and return results
        return await pagedPosts.ToListAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Validates that slug is not empty.
    /// </summary>
    private static void EnsureSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug cannot be empty.", nameof(slug));
    }
}