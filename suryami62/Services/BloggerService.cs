#region

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using suryami62.Models;

#endregion

namespace suryami62.Services;

internal interface IBloggerService
{
    Task<ServiceResult<PostList>> GetPostsAsync(string? pageToken = null);
    Task<ServiceResult<BlogPost>> GetPostByIdAsync(string postId);
    Task<ServiceResult<BlogPost>> GetPostByPathAsync(string path);
    Task<ServiceResult<PostList>> SearchPostsAsync(string query);
}

[SuppressMessage("Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated via Dependency Injection")]
internal sealed class BloggerService : IBloggerService
{
    private readonly string _apiKey;
    private readonly string _blogId;
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;

    public BloggerService(HttpClient httpClient, IMemoryCache cache, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _httpClient = httpClient;
        _cache = cache;
        _apiKey = configuration["Blogger:ApiKey"] ?? string.Empty;
        _blogId = configuration["Blogger:BlogId"] ?? string.Empty;

        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_blogId))
            throw new InvalidOperationException("Blogger configuration (BlogId or ApiKey) is missing.");
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Wrapper service designed to return failures as results")]
    public async Task<ServiceResult<PostList>> GetPostsAsync(string? pageToken = null)
    {
        var cacheKey = $"blogger_posts_{_blogId}_{pageToken ?? "default"}";
        if (_cache.TryGetValue(cacheKey, out PostList? cachedPosts))
            return ServiceResult.Ok(cachedPosts ?? new PostList());

        var url = $"{_blogId}/posts?fetchBodies=true&view=READER&key={_apiKey}";
        if (!string.IsNullOrEmpty(pageToken)) url += $"&pageToken={pageToken}";
        try
        {
            var result = await _httpClient.GetFromJsonAsync<PostList>(url).ConfigureAwait(false);
            var data = result ?? new PostList();
            _cache.Set(cacheKey, data, TimeSpan.FromMinutes(10));
            return ServiceResult.Ok(data);
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult.Fail<PostList>($"Network/API Error: {ex.Message}");
        }
        catch (Exception ex) // CA1031: Catching general exception to return failure result
        {
            return ServiceResult.Fail<PostList>($"Unexpected Error: {ex.Message}");
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Wrapper service designed to return failures as results")]
    public async Task<ServiceResult<BlogPost>> GetPostByIdAsync(string postId)
    {
        var cacheKey = $"blogger_post_{postId}";
        if (_cache.TryGetValue(cacheKey, out BlogPost? cachedPost))
            return ServiceResult.Ok(cachedPost ?? new BlogPost());

        var url = $"{_blogId}/posts/{postId}?view=READER&key={_apiKey}";
        try
        {
            var result = await _httpClient.GetFromJsonAsync<BlogPost>(url).ConfigureAwait(false);
            if (result == null) return ServiceResult.Fail<BlogPost>("Post not found (null response).");
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
            return ServiceResult.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult.Fail<BlogPost>($"Failed to load post: {ex.Message}");
        }
        catch (Exception ex) // CA1031: Catching general exception
        {
            return ServiceResult.Fail<BlogPost>($"Error: {ex.Message}");
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Wrapper service designed to return failures as results")]
    public async Task<ServiceResult<BlogPost>> GetPostByPathAsync(string path)
    {
        var url = $"{_blogId}/posts/bypath?path={path}&view=READER&key={_apiKey}";
        try
        {
            var result = await _httpClient.GetFromJsonAsync<BlogPost>(url).ConfigureAwait(false);
            if (result == null) return ServiceResult.Fail<BlogPost>("Post not found.");
            return ServiceResult.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult.Fail<BlogPost>($"Failed to load post by path: {ex.Message}");
        }
        catch (Exception ex) // CA1031: Catching general exception
        {
            return ServiceResult.Fail<BlogPost>($"Error: {ex.Message}");
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Wrapper service designed to return failures as results")]
    public async Task<ServiceResult<PostList>> SearchPostsAsync(string query)
    {
        var url = $"{_blogId}/posts/search?q={query}&fetchBodies=true&key={_apiKey}";
        try
        {
            var result = await _httpClient.GetFromJsonAsync<PostList>(url).ConfigureAwait(false);
            return ServiceResult.Ok(result ?? new PostList());
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult.Fail<PostList>($"Search failed: {ex.Message}");
        }
        catch (Exception ex) // CA1031: Catching general exception
        {
            return ServiceResult.Fail<PostList>($"Error: {ex.Message}");
        }
    }
}