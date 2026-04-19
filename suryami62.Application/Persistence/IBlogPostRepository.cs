// ============================================================================
// BLOG POST REPOSITORY INTERFACE
// ============================================================================
// This interface defines the contract for blog post data access.
//
// WHAT IS A REPOSITORY?
// A repository is a class that handles data storage and retrieval.
// The interface defines WHAT operations are available, not HOW they work.
// The actual database code (SQL) is in the Infrastructure layer.
//
// BENEFITS:
// - Easy to switch database (PostgreSQL, SQL Server, etc.)
// - Easy to test (can use fake repository)
// - Business logic doesn't depend on database details
//
// RETURNS TUPLE:
// GetPostsAsync returns (List<BlogPost> Items, int Total):
// - Items: The actual blog posts for this page
// - Total: Total count (for pagination "Showing X of Y posts")
// ============================================================================

#region

using suryami62.Domain.Models;

#endregion

namespace suryami62.Application.Persistence;

/// <summary>
///     Defines operations for storing and retrieving blog posts.
/// </summary>
public interface IBlogPostRepository
{
    /// <summary>
    ///     Gets a list of blog posts with pagination and optional filtering.
    /// </summary>
    /// <param name="onlyPublished">If true, only return published posts (default: true).</param>
    /// <param name="skip">Number of posts to skip (for pagination).</param>
    /// <param name="take">Maximum number of posts to return (page size).</param>
    /// <param name="searchTerm">Optional text to search in title/content.</param>
    /// <returns>
    ///     A tuple containing:
    ///     - Items: The list of blog posts
    ///     - Total: Total number of matching posts (for pagination)
    /// </returns>
    Task<(List<BlogPost> Items, int Total)> GetPostsAsync(
        bool onlyPublished = true,
        int? skip = null,
        int? take = null,
        string? searchTerm = null);

    /// <summary>
    ///     Gets a single blog post by its URL slug (e.g., "my-first-post").
    /// </summary>
    /// <param name="slug">The URL-friendly identifier for the post.</param>
    /// <returns>The blog post, or null if not found.</returns>
    Task<BlogPost?> GetBySlugAsync(string slug);

    /// <summary>
    ///     Checks if a slug already exists in the database.
    /// </summary>
    /// <param name="slug">The slug to check.</param>
    /// <param name="excludeId">Optional post ID to exclude from check (for updates).</param>
    /// <returns>True if slug exists; otherwise false.</returns>
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null);

    /// <summary>
    ///     Gets a single blog post by its database ID.
    /// </summary>
    /// <param name="id">The numeric database ID.</param>
    /// <returns>The blog post, or null if not found.</returns>
    Task<BlogPost?> GetByIdAsync(int id);

    /// <summary>
    ///     Creates a new blog post in the database.
    /// </summary>
    /// <param name="post">The blog post to create.</param>
    /// <returns>The created post (with assigned ID).</returns>
    Task<BlogPost> CreateAsync(BlogPost post);

    /// <summary>
    ///     Updates an existing blog post in the database.
    /// </summary>
    /// <param name="post">The blog post with updated values.</param>
    Task UpdateAsync(BlogPost post);

    /// <summary>
    ///     Deletes a blog post from the database.
    /// </summary>
    /// <param name="id">The ID of the post to delete.</param>
    Task DeleteAsync(int id);
}