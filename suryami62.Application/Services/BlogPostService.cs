// ============================================================================
// BLOG POST SERVICE
// ============================================================================
// This service manages blog posts with Redis caching for performance.
//
// WHAT IS CACHING?
// Caching stores frequently-accessed data in fast storage (Redis) to reduce
// database load. Blog posts don't change often, so caching them makes sense.
//
// CACHE-ASIDE PATTERN:
// 1. Check cache first (fast)
// 2. If not in cache (miss), fetch from database (slow)
// 3. Store in cache for next time
//
// STAMPEDE PROTECTION:
// When cache expires, many users might request the same data simultaneously.
// Without protection, all would hit the database at once (stampede).
// This uses locking so only one request fetches from database.
//
// CACHE KEYS:
// - blogposts:list:true:0:10:search  - List of posts (varies by parameters)
// - blogposts:slug:my-post          - Single post by slug
// - blogposts:id:42                  - Single post by ID
//
// CACHE INVALIDATION:
// When a post is created/updated/deleted, we clear related caches so
// the next request gets fresh data.
// ============================================================================

#region

using System.Text;
using suryami62.Application.Persistence;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Services;

/// <summary>
///     Interface for blog post operations. This allows using fake/mock
///     implementations for testing without a real database.
/// </summary>
public interface IBlogPostService
{
    /// <summary>
    ///     Gets a list of blog posts with optional filtering and pagination.
    /// </summary>
    /// <param name="onlyPublished">If true, only return published posts.</param>
    /// <param name="skip">Number of posts to skip (for pagination).</param>
    /// <param name="take">Maximum posts to return (page size).</param>
    /// <param name="searchTerm">Text to search in titles/content.</param>
    /// <returns>Tuple with (list of posts, total count).</returns>
    Task<(List<BlogPost> Items, int Total)> GetPostsAsync(
        bool onlyPublished = true,
        int? skip = null,
        int? take = null,
        string? searchTerm = null);

    /// <summary>
    ///     Gets a single post by its URL slug (e.g., "hello-world").
    /// </summary>
    /// <param name="slug">The URL-friendly post identifier.</param>
    /// <returns>The post, or null if not found.</returns>
    Task<BlogPost?> GetPostBySlugAsync(string slug);

    /// <summary>
    ///     Gets a single post by its database ID.
    /// </summary>
    /// <param name="id">The numeric database ID.</param>
    /// <returns>The post, or null if not found.</returns>
    Task<BlogPost?> GetPostByIdAsync(int id);

    /// <summary>
    ///     Creates a new blog post.
    /// </summary>
    /// <param name="post">The post to create.</param>
    /// <returns>The created post (with assigned ID).</returns>
    Task<BlogPost> CreatePostAsync(BlogPost post);

    /// <summary>
    ///     Updates an existing blog post.
    /// </summary>
    /// <param name="post">The post with updated values.</param>
    Task UpdatePostAsync(BlogPost post);

    /// <summary>
    ///     Deletes a blog post.
    /// </summary>
    /// <param name="id">The ID of the post to delete.</param>
    Task DeletePostAsync(int id);

    /// <summary>
    ///     Generates a unique slug from a title, appending a counter if needed.
    /// </summary>
    /// <param name="title">The post title to generate slug from.</param>
    /// <param name="excludeId">Optional post ID to exclude from uniqueness check.</param>
    /// <returns>A unique slug string.</returns>
    Task<string> GenerateUniqueSlugAsync(string title, int? excludeId = null);

    /// <summary>
    ///     Checks if a slug is already in use.
    /// </summary>
    /// <param name="slug">The slug to check.</param>
    /// <param name="excludeId">Optional post ID to exclude from check.</param>
    /// <returns>True if slug exists; otherwise false.</returns>
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
}

/// <summary>
///     Implements blog post operations with Redis caching.
///     Uses cache-aside pattern with stampede protection for high-traffic scenarios.
/// </summary>
public sealed class BlogPostService : IBlogPostService
{
    // All cache keys start with this prefix for organization
    private const string CacheKeyPrefix = "blogposts:";

    // Cache entries expire after 15 minutes
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

    // Optional cache service (null if Redis is not configured)
    private readonly IRedisCacheService? _cacheService;

    // Repository for database operations
    private readonly IBlogPostRepository _repository;

    // Optional stampede protection (prevents database overload)
    private readonly CacheStampedeProtection? _stampedeProtection;

    /// <summary>
    ///     Creates a new blog post service.
    /// </summary>
    /// <param name="repository">The database repository for blog posts.</param>
    /// <param name="cacheService">Optional Redis cache service.</param>
    /// <param name="stampedeProtection">Optional stampede protection locking.</param>
    public BlogPostService(
        IBlogPostRepository repository,
        IRedisCacheService? cacheService = null,
        CacheStampedeProtection? stampedeProtection = null)
    {
        _repository = repository;
        _cacheService = cacheService;
        _stampedeProtection = stampedeProtection;
    }

    /// <summary>
    ///     Gets a list of blog posts with optional filtering.
    ///     This is the most complex method because it uses stampede protection.
    /// </summary>
    public async Task<(List<BlogPost> Items, int Total)> GetPostsAsync(
        bool onlyPublished = true,
        int? skip = null,
        int? take = null,
        string? searchTerm = null)
    {
        // Step 1: Build cache key that uniquely identifies this query
        // Example: "blogposts:list:true:0:10:hello"
        var cacheKey = $"{CacheKeyPrefix}list:{onlyPublished}:{skip ?? 0}:{take ?? 0}:{searchTerm ?? ""}";

        // Step 2: Try to get from cache first (cache-aside pattern)
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<CachedBlogPostList>(cacheKey)
                .ConfigureAwait(false);

            if (cached != null)
                // Cache hit - return immediately (fast!)
                return (cached.Items, cached.Total);
        }

        // Step 3: Cache miss - need to fetch from database
        // Use stampede protection if available to prevent database overload
        if (_stampedeProtection != null && _cacheService != null)
        {
            // Execute with locking - only one thread fetches from database
            var result = await _stampedeProtection
                .ExecuteAsync(cacheKey, async () =>
                {
                    // Step 3a: Double-check cache (another thread might have populated it)
                    var doubleCheck = await _cacheService
                        .GetAsync<CachedBlogPostList>(cacheKey)
                        .ConfigureAwait(false);

                    if (doubleCheck != null) return (doubleCheck.Items, doubleCheck.Total);

                    // Step 3b: Fetch from database (expensive operation)
                    var dbResult = await _repository
                        .GetPostsAsync(onlyPublished, skip, take, searchTerm)
                        .ConfigureAwait(false);

                    // Step 3c: Store in cache for future requests
                    await _cacheService.SetAsync(
                        cacheKey,
                        new CachedBlogPostList(dbResult.Items, dbResult.Total),
                        CacheExpiration).ConfigureAwait(false);

                    return dbResult;
                }).ConfigureAwait(false);

            return result;
        }

        // Step 4: No stampede protection - fetch directly from database
        var fallbackResult = await _repository
            .GetPostsAsync(onlyPublished, skip, take, searchTerm)
            .ConfigureAwait(false);

        // Store in cache if caching is available
        if (_cacheService != null)
            await _cacheService.SetAsync(
                cacheKey,
                new CachedBlogPostList(fallbackResult.Items, fallbackResult.Total),
                CacheExpiration).ConfigureAwait(false);

        return fallbackResult;
    }

    /// <summary>
    ///     Gets a single post by its URL slug.
    /// </summary>
    public async Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(slug)) return null;

        // Build cache key
        var cacheKey = $"{CacheKeyPrefix}slug:{slug}";

        // Step 1: Try cache first
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<BlogPost>(cacheKey).ConfigureAwait(false);
            if (cached != null) return cached;
        }

        // Step 2: Cache miss - fetch from database
        var post = await _repository.GetBySlugAsync(slug).ConfigureAwait(false);

        // Step 3: Store in cache if found
        if (_cacheService != null && post != null)
            await _cacheService.SetAsync(cacheKey, post, CacheExpiration).ConfigureAwait(false);

        return post;
    }

    /// <summary>
    ///     Gets a single post by its database ID.
    /// </summary>
    public async Task<BlogPost?> GetPostByIdAsync(int id)
    {
        // Build cache key
        var cacheKey = $"{CacheKeyPrefix}id:{id}";

        // Step 1: Try cache first
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<BlogPost>(cacheKey).ConfigureAwait(false);
            if (cached != null) return cached;
        }

        // Step 2: Cache miss - fetch from database
        var post = await _repository.GetByIdAsync(id).ConfigureAwait(false);

        // Step 3: Store in cache if found
        if (_cacheService != null && post != null)
            await _cacheService.SetAsync(cacheKey, post, CacheExpiration).ConfigureAwait(false);

        return post;
    }

    /// <summary>
    ///     Creates a new blog post and invalidates list caches.
    ///     Validates slug uniqueness before saving.
    /// </summary>
    public async Task<BlogPost> CreatePostAsync(BlogPost post)
    {
        ArgumentNullException.ThrowIfNull(post);

        // Validate slug is not empty
        if (string.IsNullOrWhiteSpace(post.Slug))
            throw new ArgumentException("Post slug cannot be empty.", nameof(post));

        // Check for duplicate slug
        if (await _repository.SlugExistsAsync(post.Slug).ConfigureAwait(false))
            throw new InvalidOperationException($"A post with slug '{post.Slug}' already exists.");

        // Save to database first
        var result = await _repository.CreateAsync(post).ConfigureAwait(false);

        // Invalidate list caches (the new post should appear in lists)
        if (_cacheService != null)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*").ConfigureAwait(false);

        return result;
    }

    /// <summary>
    ///     Updates an existing post and invalidates related caches.
    ///     Validates slug uniqueness before saving.
    /// </summary>
    public async Task UpdatePostAsync(BlogPost post)
    {
        ArgumentNullException.ThrowIfNull(post);

        // Validate slug is not empty
        if (string.IsNullOrWhiteSpace(post.Slug))
            throw new ArgumentException("Post slug cannot be empty.", nameof(post));

        // Check for duplicate slug (exclude current post)
        if (await _repository.SlugExistsAsync(post.Slug, post.Id).ConfigureAwait(false))
            throw new InvalidOperationException($"A post with slug '{post.Slug}' already exists.");

        // Update in database
        await _repository.UpdateAsync(post).ConfigureAwait(false);

        // Invalidate related caches so next request gets fresh data
        if (_cacheService != null)
        {
            // Remove specific post caches
            await _cacheService.RemoveEntryAsync($"{CacheKeyPrefix}slug:{post.Slug}")
                .ConfigureAwait(false);
            await _cacheService.RemoveEntryAsync($"{CacheKeyPrefix}id:{post.Id}")
                .ConfigureAwait(false);

            // Remove all list caches (post might appear in lists)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*")
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Deletes a post and invalidates related caches.
    /// </summary>
    public async Task DeletePostAsync(int id)
    {
        // Get post first (needed to invalidate slug-based cache)
        var post = await _repository.GetByIdAsync(id).ConfigureAwait(false);

        // Delete from database
        await _repository.DeleteAsync(id).ConfigureAwait(false);

        // Invalidate related caches
        if (_cacheService != null)
        {
            // Remove slug cache if we know the slug
            if (post != null)
                await _cacheService.RemoveEntryAsync($"{CacheKeyPrefix}slug:{post.Slug}")
                    .ConfigureAwait(false);

            // Remove ID cache
            await _cacheService.RemoveEntryAsync($"{CacheKeyPrefix}id:{id}")
                .ConfigureAwait(false);

            // Remove all list caches (post no longer appears in lists)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*")
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Generates a unique slug from a title, appending a counter if needed.
    ///     Example: "hello-world", "hello-world-2", "hello-world-3"
    /// </summary>
    public async Task<string> GenerateUniqueSlugAsync(string title, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        // Generate base slug from title
        var baseSlug = CreateSlug(title);
        if (string.IsNullOrEmpty(baseSlug))
            return string.Empty;

        // Check if base slug is unique
        if (!await _repository.SlugExistsAsync(baseSlug, excludeId).ConfigureAwait(false))
            return baseSlug;

        // Find next available number
        var counter = 2;
        string candidate;
        do
        {
            candidate = $"{baseSlug}-{counter}";
            counter++;
        } while (await _repository.SlugExistsAsync(candidate, excludeId).ConfigureAwait(false));

        return candidate;
    }

    /// <summary>
    ///     Checks if a slug is already in use.
    /// </summary>
    public Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
    {
        return _repository.SlugExistsAsync(slug, excludeId);
    }

    /// <summary>
    ///     Converts a title to a URL-friendly slug.
    ///     Removes special characters, converts to lowercase, spaces to hyphens.
    /// </summary>
    private static string CreateSlug(string title)
    {
        var builder = new StringBuilder(title.Length);
        var previousWasDash = false;

        foreach (var c in title.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                previousWasDash = false;
                continue;
            }

            if (char.IsWhiteSpace(c) || c is '-' or '_')
                if (builder.Length > 0 && !previousWasDash)
                {
                    builder.Append('-');
                    previousWasDash = true;
                }
        }

        return builder.ToString().Trim('-');
    }

    /// <summary>
    ///     Helper class for caching paginated list results.
    ///     Stores both the items and total count needed for pagination.
    /// </summary>
    private sealed record CachedBlogPostList(List<BlogPost> Items, int Total);
}