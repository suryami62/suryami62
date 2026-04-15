// ============================================================================
// BLOG POST MODEL
// ============================================================================
// This class represents a blog post stored in the database.
// It is used both by Entity Framework Core (to create database tables)
// and by the application code to display posts.
//
// WHAT IS A "MODEL"?
// A model is a class that represents data in your application.
// Think of it like a blueprint or template that defines what data
// a blog post contains (title, content, date, etc.).
//
// ENTITY FRAMEWORK CORE (EF Core):
// EF Core uses this class to:
// - Create the database table structure
// - Map database rows to C# objects
// - Track changes and save them back to database
//
// DATA ANNOTATIONS:
// [Required] - This field must have a value (cannot be null/empty)
// [StringLength(200)] - Maximum length of 200 characters
//   ^ Prevents database errors and validates user input
//
// INITIALIZATION:
// Properties like "Title { get; set; } = string.Empty" initialize
// with an empty string to avoid null reference exceptions.
// ============================================================================

#region

using System.ComponentModel.DataAnnotations;

#endregion

namespace suryami62.Domain.Models;

/// <summary>
///     Represents a blog post with title, content, and publication details.
///     This is the main data model for blog content.
/// </summary>
public sealed class BlogPost
{
    /// <summary>
    ///     Unique identifier for this blog post.
    ///     Database auto-generates this when post is created.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     The title shown at the top of the post.
    ///     Required field - cannot be empty.
    ///     Maximum length: 200 characters (defined in DomainModelConstraints).
    /// </summary>
    [Required]
    [StringLength(DomainModelConstraints.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     URL-friendly version of the title used in links.
    ///     Example: "My First Post" -> "my-first-post"
    ///     Required field - used to find post via URL.
    ///     Maximum length: 250 characters.
    /// </summary>
    [Required]
    [StringLength(DomainModelConstraints.BlogPostSlugMaxLength)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    ///     The main content of the blog post.
    ///     Can be Markdown (text with formatting codes) or HTML.
    ///     Required field - cannot be empty.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    ///     Category or tag shown with the post (e.g., "Technology", "Tutorial").
    ///     Required field - helps organize posts by topic.
    /// </summary>
    [Required]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     The date when this post was published or last updated.
    ///     Default value is the current UTC time when post is created.
    /// </summary>
    public DateTime Date { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Short summary shown in post listings (not full post page).
    ///     Required field - helps readers decide to click.
    /// </summary>
    [Required]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    ///     Whether this post is publicly visible.
    ///     False = draft (only visible to admin).
    ///     True = published (visible to everyone).
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    ///     Optional URL to the main image/cover photo for this post.
    ///     Can be null if post has no featured image.
    ///     Example: https://example.com/images/post-cover.jpg
    /// </summary>
    public Uri? ImageUrl { get; set; }
}