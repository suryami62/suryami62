#region

using System.ComponentModel.DataAnnotations;

#endregion

namespace suryami62.Domain.Models;

/// <summary>
///     Represents a blog post displayed on the public site and managed from the admin area.
/// </summary>
public sealed class BlogPost
{
    /// <summary>
    ///     Gets or sets the unique identifier of the post.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the display title of the post.
    /// </summary>
    [Required] [StringLength(200)] public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the URL-friendly slug used in the route.
    /// </summary>
    [Required] [StringLength(250)] public string Slug { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the markdown or HTML content of the post.
    /// </summary>
    [Required] public string Content { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the category or label shown with the post.
    /// </summary>
    [Required] public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the publication date associated with the post.
    /// </summary>
    public DateTime Date { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets the short summary used in listings and previews.
    /// </summary>
    [Required] public string Summary { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether the post is publicly visible.
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    ///     Gets or sets the optional cover image URL for the post.
    /// </summary>
    public Uri? ImageUrl { get; set; }
}