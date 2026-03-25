#region

using suryami62.Application.Persistence;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Services;

/// <summary>
///     Defines application operations for managing blog posts.
/// </summary>
public interface IBlogPostService
{
    /// <summary>
    ///     Gets blog posts that match the supplied filter and paging arguments.
    /// </summary>
    /// <param name="onlyPublished">Restricts the result to published posts when true.</param>
    /// <param name="skip">The optional number of items to skip.</param>
    /// <param name="take">The optional maximum number of items to return.</param>
    /// <param name="searchTerm">The optional search term applied to titles and summaries.</param>
    /// <returns>A tuple containing the matching items and total count.</returns>
    Task<(List<BlogPost> Items, int Total)>
        GetPostsAsync(bool onlyPublished = true, int? skip = null, int? take = null, string? searchTerm = null);

    /// <summary>
    ///     Gets a blog post by its slug.
    /// </summary>
    /// <param name="slug">The route slug of the post.</param>
    /// <returns>The matching post when found; otherwise <see langword="null" />.</returns>
    Task<BlogPost?> GetPostBySlugAsync(string slug);

    /// <summary>
    ///     Gets a blog post by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the post.</param>
    /// <returns>The matching post when found; otherwise <see langword="null" />.</returns>
    Task<BlogPost?> GetPostByIdAsync(int id);

    /// <summary>
    ///     Creates a new blog post.
    /// </summary>
    /// <param name="post">The post to persist.</param>
    /// <returns>The created post.</returns>
    Task<BlogPost> CreatePostAsync(BlogPost post);

    /// <summary>
    ///     Updates an existing blog post.
    /// </summary>
    /// <param name="post">The post to update.</param>
    Task UpdatePostAsync(BlogPost post);

    /// <summary>
    ///     Deletes a blog post by identifier.
    /// </summary>
    /// <param name="id">The identifier of the post to remove.</param>
    Task DeletePostAsync(int id);
}

/// <summary>
///     Implements blog post operations by delegating to the configured repository.
/// </summary>
public sealed class BlogPostService(IBlogPostRepository repository) : IBlogPostService
{
    /// <inheritdoc />
    public Task<(List<BlogPost> Items, int Total)> GetPostsAsync(
        bool onlyPublished = true,
        int? skip = null,
        int? take = null,
        string? searchTerm = null)
    {
        return repository.GetPostsAsync(onlyPublished, skip, take, searchTerm);
    }

    /// <inheritdoc />
    public Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        return repository.GetBySlugAsync(slug);
    }

    /// <inheritdoc />
    public Task<BlogPost?> GetPostByIdAsync(int id)
    {
        return repository.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public Task<BlogPost> CreatePostAsync(BlogPost post)
    {
        return repository.CreateAsync(post);
    }

    /// <inheritdoc />
    public Task UpdatePostAsync(BlogPost post)
    {
        return repository.UpdateAsync(post);
    }

    /// <inheritdoc />
    public Task DeletePostAsync(int id)
    {
        return repository.DeleteAsync(id);
    }
}