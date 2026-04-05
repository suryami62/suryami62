#region

using suryami62.Application.Persistence;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Services;

/// <summary>
///     Defines application operations for managing blog posts.
/// </summary>
public interface IBlogPostService
{
    /// <summary>
    ///     Gets blog posts that match the supplied filter and paging arguments.
    /// </summary>
    /// <param name="onlyPublished">Restricts the result to published posts when true.</param>
    /// <param name="skip">The optional number of items to skip.</param>
    /// <param name="take">The optional maximum number of items to return.</param>
    /// <param name="searchTerm">The optional search term applied to titles and summaries.</param>
    /// <returns>A tuple containing the matching items and total count.</returns>
    Task<(List<BlogPost> Items, int Total)>
        GetPostsAsync(bool onlyPublished = true, int? skip = null, int? take = null, string? searchTerm = null);

    /// <summary>
    ///     Gets a blog post by its slug.
    /// </summary>
    /// <param name="slug">The route slug of the post.</param>
    /// <returns>The matching post when found; otherwise <see langword="null" />.</returns>
    Task<BlogPost?> GetPostBySlugAsync(string slug);

    /// <summary>
    ///     Gets a blog post by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the post.</param>
    /// <returns>The matching post when found; otherwise <see langword="null" />.</returns>
    Task<BlogPost?> GetPostByIdAsync(int id);

    /// <summary>
    ///     Creates a new blog post.
    /// </summary>
    /// <param name="post">The post to persist.</param>
    /// <returns>The created post.</returns>
    Task<BlogPost> CreatePostAsync(BlogPost post);

    /// <summary>
    ///     Updates an existing blog post.
    /// </summary>
    /// <param name="post">The post to update.</param>
    Task UpdatePostAsync(BlogPost post);

    /// <summary>
    ///     Deletes a blog post by identifier.
    /// </summary>
    /// <param name="id">The identifier of the post to remove.</param>
    Task DeletePostAsync(int id);
}

/// <summary>
///     Implements blog post operations with Redis caching support.
/// </summary>
public sealed class BlogPostService : IBlogPostService
{
    private const string CacheKeyPrefix = "blogposts:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);
    private readonly IRedisCacheService? _cacheService;

    private readonly IBlogPostRepository _repository;

    public BlogPostService(IBlogPostRepository repository, IRedisCacheService? cacheService = null)
    {
        _repository = repository;
        _cacheService = cacheService;
    }

    /// <inheritdoc />
    public async Task<(List<BlogPost> Items, int Total)> GetPostsAsync(
        bool onlyPublished = true,
        int? skip = null,
        int? take = null,
        string? searchTerm = null)
    {
        // Create cache key based on parameters
        var cacheKey = $"{CacheKeyPrefix}list:{onlyPublished}:{skip ?? 0}:{take ?? 0}:{searchTerm ?? ""}";

        // Try to get from cache first (cache-aside pattern)
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<CachedBlogPostList>(cacheKey).ConfigureAwait(false);
            if (cached != null) return (cached.Items, cached.Total);
        }

        // Cache miss - fetch from database
        var result = await _repository.GetPostsAsync(onlyPublished, skip, take, searchTerm).ConfigureAwait(false);

        // Store in cache
        if (_cacheService != null)
            await _cacheService.SetAsync(cacheKey, new CachedBlogPostList(result.Items, result.Total), CacheExpiration)
                .ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;

        var cacheKey = $"{CacheKeyPrefix}slug:{slug}";

        // Try to get from cache first
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<BlogPost>(cacheKey).ConfigureAwait(false);
            if (cached != null) return cached;
        }

        // Cache miss - fetch from database
        var post = await _repository.GetBySlugAsync(slug).ConfigureAwait(false);

        // Store in cache if found
        if (_cacheService != null && post != null)
            await _cacheService.SetAsync(cacheKey, post, CacheExpiration).ConfigureAwait(false);

        return post;
    }

    /// <inheritdoc />
    public async Task<BlogPost?> GetPostByIdAsync(int id)
    {
        var cacheKey = $"{CacheKeyPrefix}id:{id}";

        // Try to get from cache first
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<BlogPost>(cacheKey).ConfigureAwait(false);
            if (cached != null) return cached;
        }

        // Cache miss - fetch from database
        var post = await _repository.GetByIdAsync(id).ConfigureAwait(false);

        // Store in cache if found
        if (_cacheService != null && post != null)
            await _cacheService.SetAsync(cacheKey, post, CacheExpiration).ConfigureAwait(false);

        return post;
    }

    /// <inheritdoc />
    public async Task<BlogPost> CreatePostAsync(BlogPost post)
    {
        var result = await _repository.CreateAsync(post).ConfigureAwait(false);

        // Invalidate list caches
        if (_cacheService != null)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*").ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async Task UpdatePostAsync(BlogPost post)
    {
        await _repository.UpdateAsync(post).ConfigureAwait(false);

        // Invalidate related caches
        if (_cacheService != null)
        {
            await _cacheService.RemoveAsync($"{CacheKeyPrefix}slug:{post.Slug}").ConfigureAwait(false);
            await _cacheService.RemoveAsync($"{CacheKeyPrefix}id:{post.Id}").ConfigureAwait(false);
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*").ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task DeletePostAsync(int id)
    {
        // Get post first for cache invalidation
        var post = await _repository.GetByIdAsync(id).ConfigureAwait(false);

        await _repository.DeleteAsync(id).ConfigureAwait(false);

        // Invalidate caches
        if (_cacheService != null)
        {
            if (post != null)
                await _cacheService.RemoveAsync($"{CacheKeyPrefix}slug:{post.Slug}").ConfigureAwait(false);
            await _cacheService.RemoveAsync($"{CacheKeyPrefix}id:{id}").ConfigureAwait(false);
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*").ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Internal type for caching paginated lists.
    /// </summary>
    private sealed record CachedBlogPostList(List<BlogPost> Items, int Total);
}