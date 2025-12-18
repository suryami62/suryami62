#region

using System.Text.Json.Serialization;

#endregion

namespace suryami62.Models;

public class BlogPost
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;

    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;

    [JsonPropertyName("published")] public DateTime Published { get; set; }

    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
}

public class PostList
{
    [JsonPropertyName("items")] public List<BlogPost>? Items { get; set; }

    [JsonPropertyName("nextPageToken")] public string? NextPageToken { get; set; }
}