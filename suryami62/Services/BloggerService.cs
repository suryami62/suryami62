using System.Net.Http.Json;
using suryami62.Models;

namespace suryami62.Services
{
    public interface IBloggerService
    {
        Task<PostList> GetPostsAsync();
        Task<BlogPost?> GetPostByIdAsync(string postId);
        Task<BlogPost?> GetPostByPathAsync(string path);
        Task<PostList> SearchPostsAsync(string query);
    }

    public class BloggerService : IBloggerService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _blogId;
        private const string BaseUrl = "https://blogger.googleapis.com/v3/blogs";

        public BloggerService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Blogger:ApiKey"] ?? string.Empty;
            _blogId = configuration["Blogger:BlogId"] ?? string.Empty;
        }

        public async Task<PostList> GetPostsAsync()
        {
            if (string.IsNullOrEmpty(_blogId) || string.IsNullOrEmpty(_apiKey))
            {
                return new PostList();
            }

            var url = $"{BaseUrl}/{_blogId}/posts?fetchBodies=true&view=READER&key={_apiKey}";
            try
            {
                var result = await _httpClient.GetFromJsonAsync<PostList>(url);
                return result ?? new PostList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching posts: {ex.Message}");
                return new PostList();
            }
        }

        public async Task<BlogPost?> GetPostByIdAsync(string postId)
        {
            if (string.IsNullOrEmpty(_blogId) || string.IsNullOrEmpty(_apiKey))
            {
                return null;
            }

            var url = $"{BaseUrl}/{_blogId}/posts/{postId}?view=READER&key={_apiKey}";
            try
            {
                return await _httpClient.GetFromJsonAsync<BlogPost>(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching post {postId}: {ex.Message}");
                return null;
            }
        }

        public async Task<BlogPost?> GetPostByPathAsync(string path)
        {
            if (string.IsNullOrEmpty(_blogId) || string.IsNullOrEmpty(_apiKey))
            {
                return null;
            }

            // Ensure path starts with / if not present, though usually passed correctly.
            // Blogger API expects path parameter.
            var url = $"{BaseUrl}/{_blogId}/posts/bypath?path={path}&view=READER&key={_apiKey}";
            try
            {
                return await _httpClient.GetFromJsonAsync<BlogPost>(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching post by path {path}: {ex.Message}");
                return null;
            }
        }

        public async Task<PostList> SearchPostsAsync(string query)
        {
            if (string.IsNullOrEmpty(_blogId) || string.IsNullOrEmpty(_apiKey))
            {
                return new PostList();
            }

            var url = $"{BaseUrl}/{_blogId}/posts/search?q={query}&fetchBodies=true&key={_apiKey}";
            try
            {
                var result = await _httpClient.GetFromJsonAsync<PostList>(url);
                return result ?? new PostList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching posts with query {query}: {ex.Message}");
                return new PostList();
            }
        }
    }
}
