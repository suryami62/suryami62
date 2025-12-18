#region

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

#endregion

namespace suryami62.Models;

internal sealed class BlogPost
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;

    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;

    [JsonPropertyName("published")] public DateTime Published { get; set; }

    [JsonPropertyName("url")] public Uri? Url { get; set; }
}

internal sealed class PostList
{
    [JsonPropertyName("items")] public List<BlogPost>? Items { get; set; }

    [JsonPropertyName("nextPageToken")] public string? NextPageToken { get; set; }
}