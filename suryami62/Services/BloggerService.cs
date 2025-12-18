using Microsoft.Extensions.Caching.Memory;
using suryami62.Models;

namespace suryami62.Services;

public interface IBloggerService
{
    Task<ServiceResult<PostList>> GetPostsAsync(string? pageToken = null);
    Task<ServiceResult<BlogPost>> GetPostByIdAsync(string postId);
    Task<ServiceResult<BlogPost>> GetPostByPathAsync(string path);
    Task<ServiceResult<PostList>> SearchPostsAsync(string query);
}

public class BloggerService : IBloggerService
{
    private const string BaseUrl = "https://blogger.googleapis.com/v3/blogs";
    private readonly string _apiKey;
    private readonly string _blogId;
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;

    public BloggerService(HttpClient httpClient, IMemoryCache cache, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _cache = cache;
        _apiKey = configuration["Blogger:ApiKey"] ?? string.Empty;
        _blogId = configuration["Blogger:BlogId"] ?? string.Empty;
    }

    public async Task<ServiceResult<PostList>> GetPostsAsync(string? pageToken = null)
    {
        if (string.IsNullOrEmpty(_blogId) || string.IsNullOrEmpty(_apiKey))
            return ServiceResult<PostList>.Fail("Configuration missing: BlogId or ApiKey not set.");

        var cacheKey = $"blogger_posts_{_blogId}_{(pageToken ?? "default")}";
        if (_cache.TryGetValue(cacheKey, out PostList? cachedPosts))
            return ServiceResult<PostList>.Ok(cachedPosts ?? new PostList());

        var url = $"{BaseUrl}/{_blogId}/posts?fetchBodies=true&view=READER&key={_apiKey}";
        if (!string.IsNullOrEmpty(pageToken))
        {
            url += $"&pageToken={pageToken}";
        }
        try
        {
            var result = await _httpClient.GetFromJsonAsync<PostList>(url);
            var data = result ?? new PostList();
            _cache.Set(cacheKey, data, TimeSpan.FromMinutes(10));
            return ServiceResult<PostList>.Ok(data);
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult<PostList>.Fail($"Network/API Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ServiceResult<PostList>.Fail($"Unexpected Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<BlogPost>> GetPostByIdAsync(string postId)
    {
        if (string.IsNullOrEmpty(_blogId) || string.IsNullOrEmpty(_apiKey))
            return ServiceResult<BlogPost>.Fail("Configuration missing.");

        var cacheKey = $"blogger_post_{postId}";
        if (_cache.TryGetValue(cacheKey, out BlogPost? cachedPost))
            return ServiceResult<BlogPost>.Ok(cachedPost ?? new BlogPost());

        var url = $"{BaseUrl}/{_blogId}/posts/{postId}?view=READER&key={_apiKey}";
        try
        {
            var result = await _httpClient.GetFromJsonAsync<BlogPost>(url);
            if (result == null) return ServiceResult<BlogPost>.Fail("Post not found (null response).");
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
            return ServiceResult<BlogPost>.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult<BlogPost>.Fail($"Failed to load post: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ServiceResult<BlogPost>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<BlogPost>> GetPostByPathAsync(string path)
    {
        if (string.IsNullOrEmpty(_blogId) || string.IsNullOrEmpty(_apiKey))
            return ServiceResult<BlogPost>.Fail("Configuration missing.");

        // Ensure path starts with / if not present
        var url = $"{BaseUrl}/{_blogId}/posts/bypath?path={path}&view=READER&key={_apiKey}";
        try
        {
            var result = await _httpClient.GetFromJsonAsync<BlogPost>(url);
            if (result == null) return ServiceResult<BlogPost>.Fail("Post not found.");
            return ServiceResult<BlogPost>.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult<BlogPost>.Fail($"Failed to load post by path: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ServiceResult<BlogPost>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<PostList>> SearchPostsAsync(string query)
    {
        if (string.IsNullOrEmpty(_blogId) || string.IsNullOrEmpty(_apiKey))
            return ServiceResult<PostList>.Fail("Configuration missing.");

        var url = $"{BaseUrl}/{_blogId}/posts/search?q={query}&fetchBodies=true&key={_apiKey}";
        try
        {
            var result = await _httpClient.GetFromJsonAsync<PostList>(url);
            return ServiceResult<PostList>.Ok(result ?? new PostList());
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult<PostList>.Fail($"Search failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ServiceResult<PostList>.Fail($"Error: {ex.Message}");
        }
    }
}