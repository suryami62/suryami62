#region

using System.Text;
using suryami62.Application.Persistence;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Services;

public interface IBlogPostService
{
    Task<(List<BlogPost> Items, int Total)> GetPostsAsync(
        bool onlyPublished = true,
        int? skip = null,
        int? take = null,
        string? searchTerm = null);

    Task<BlogPost?> GetPostBySlugAsync(string slug);

    Task<BlogPost?> GetPostByIdAsync(int id);

    Task<BlogPost> CreatePostAsync(BlogPost post);

    Task UpdatePostAsync(BlogPost post);

    Task DeletePostAsync(int id);

    Task<string> GenerateUniqueSlugAsync(string title, int? excludeId = null);

    Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
}

public sealed class BlogPostService : IBlogPostService
{
    private const string CacheKeyPrefix = "blogposts:";

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

    private readonly IRedisCacheService? _cacheService;

    private readonly IBlogPostRepository _repository;

    private readonly CacheStampedeProtection? _stampedeProtection;

    public BlogPostService(
        IBlogPostRepository repository,
        IRedisCacheService? cacheService = null,
        CacheStampedeProtection? stampedeProtection = null)
    {
        _repository = repository;
        _cacheService = cacheService;
        _stampedeProtection = stampedeProtection;
    }

    public async Task<(List<BlogPost> Items, int Total)> GetPostsAsync(
        bool onlyPublished = true,
        int? skip = null,
        int? take = null,
        string? searchTerm = null)
    {
        var cacheKey = $"{CacheKeyPrefix}list:{onlyPublished}:{skip ?? 0}:{take ?? 0}:{searchTerm ?? ""}";

        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<CachedBlogPostList>(cacheKey)
                .ConfigureAwait(false);

            if (cached != null)
                return (cached.Items, cached.Total);
        }

        if (_stampedeProtection != null && _cacheService != null)
        {
            var result = await _stampedeProtection
                .ExecuteAsync(cacheKey, async () =>
                {
                    var doubleCheck = await _cacheService
                        .GetAsync<CachedBlogPostList>(cacheKey)
                        .ConfigureAwait(false);

                    if (doubleCheck != null) return (doubleCheck.Items, doubleCheck.Total);

                    var dbResult = await _repository
                        .GetPostsAsync(onlyPublished, skip, take, searchTerm)
                        .ConfigureAwait(false);

                    await _cacheService.SetAsync(
                        cacheKey,
                        new CachedBlogPostList(dbResult.Items, dbResult.Total),
                        CacheExpiration).ConfigureAwait(false);

                    return dbResult;
                }).ConfigureAwait(false);

            return result;
        }

        var fallbackResult = await _repository
            .GetPostsAsync(onlyPublished, skip, take, searchTerm)
            .ConfigureAwait(false);

        if (_cacheService != null)
            await _cacheService.SetAsync(
                cacheKey,
                new CachedBlogPostList(fallbackResult.Items, fallbackResult.Total),
                CacheExpiration).ConfigureAwait(false);

        return fallbackResult;
    }

    public async Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;

        var cacheKey = $"{CacheKeyPrefix}slug:{slug}";

        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<BlogPost>(cacheKey).ConfigureAwait(false);
            if (cached != null) return cached;
        }

        var post = await _repository.GetBySlugAsync(slug).ConfigureAwait(false);

        if (_cacheService != null && post != null)
            await _cacheService.SetAsync(cacheKey, post, CacheExpiration).ConfigureAwait(false);

        return post;
    }

    public async Task<BlogPost?> GetPostByIdAsync(int id)
    {
        var cacheKey = $"{CacheKeyPrefix}id:{id}";

        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<BlogPost>(cacheKey).ConfigureAwait(false);
            if (cached != null) return cached;
        }

        var post = await _repository.GetByIdAsync(id).ConfigureAwait(false);

        if (_cacheService != null && post != null)
            await _cacheService.SetAsync(cacheKey, post, CacheExpiration).ConfigureAwait(false);

        return post;
    }

    public async Task<BlogPost> CreatePostAsync(BlogPost post)
    {
        ArgumentNullException.ThrowIfNull(post);

        if (string.IsNullOrWhiteSpace(post.Slug))
            throw new ArgumentException("Post slug cannot be empty.", nameof(post));

        if (await _repository.SlugExistsAsync(post.Slug).ConfigureAwait(false))
            throw new InvalidOperationException($"A post with slug '{post.Slug}' already exists.");

        var result = await _repository.CreateAsync(post).ConfigureAwait(false);

        if (_cacheService != null)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*").ConfigureAwait(false);

        return result;
    }

    public async Task UpdatePostAsync(BlogPost post)
    {
        ArgumentNullException.ThrowIfNull(post);

        if (string.IsNullOrWhiteSpace(post.Slug))
            throw new ArgumentException("Post slug cannot be empty.", nameof(post));

        if (await _repository.SlugExistsAsync(post.Slug, post.Id).ConfigureAwait(false))
            throw new InvalidOperationException($"A post with slug '{post.Slug}' already exists.");

        await _repository.UpdateAsync(post).ConfigureAwait(false);

        if (_cacheService != null)
        {
            await _cacheService.RemoveEntryAsync($"{CacheKeyPrefix}slug:{post.Slug}")
                .ConfigureAwait(false);
            await _cacheService.RemoveEntryAsync($"{CacheKeyPrefix}id:{post.Id}")
                .ConfigureAwait(false);

            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*")
                .ConfigureAwait(false);
        }
    }

    public async Task DeletePostAsync(int id)
    {
        var post = await _repository.GetByIdAsync(id).ConfigureAwait(false);

        await _repository.DeleteAsync(id).ConfigureAwait(false);

        if (_cacheService != null)
        {
            if (post != null)
                await _cacheService.RemoveEntryAsync($"{CacheKeyPrefix}slug:{post.Slug}")
                    .ConfigureAwait(false);

            await _cacheService.RemoveEntryAsync($"{CacheKeyPrefix}id:{id}")
                .ConfigureAwait(false);

            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*")
                .ConfigureAwait(false);
        }
    }

    public async Task<string> GenerateUniqueSlugAsync(string title, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        var baseSlug = CreateSlug(title);
        if (string.IsNullOrEmpty(baseSlug))
            return string.Empty;

        if (!await _repository.SlugExistsAsync(baseSlug, excludeId).ConfigureAwait(false))
            return baseSlug;

        var counter = 2;
        string candidate;
        do
        {
            candidate = $"{baseSlug}-{counter}";
            counter++;
        } while (await _repository.SlugExistsAsync(candidate, excludeId).ConfigureAwait(false));

        return candidate;
    }

    public Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
    {
        return _repository.SlugExistsAsync(slug, excludeId);
    }

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

    private sealed record CachedBlogPostList(List<BlogPost> Items, int Total);
}