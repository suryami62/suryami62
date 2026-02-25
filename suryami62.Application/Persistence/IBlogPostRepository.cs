#region

using suryami62.Domain.Models;

#endregion

namespace suryami62.Application.Persistence;

/// <summary>
///     Provides persistence operations for <see cref="BlogPost" /> entities.
/// </summary>
public interface IBlogPostRepository
{
    /// <summary>
    ///     Gets a page of blog posts and the total matching count.
    /// </summary>
    Task<(List<BlogPost> Items, int Total)> GetPostsAsync(
        bool onlyPublished = true,
        int? skip = null,
        int? take = null,
        string? searchTerm = null);

    /// <summary>
    ///     Gets a post by its slug.
    /// </summary>
    Task<BlogPost?> GetBySlugAsync(string slug);

    /// <summary>
    ///     Gets a post by its identifier.
    /// </summary>
    Task<BlogPost?> GetByIdAsync(int id);

    /// <summary>
    ///     Creates a post.
    /// </summary>
    Task<BlogPost> CreateAsync(BlogPost post);

    /// <summary>
    ///     Updates a post.
    /// </summary>
    Task UpdateAsync(BlogPost post);

    /// <summary>
    ///     Deletes a post.
    /// </summary>
    Task DeleteAsync(int id);
}