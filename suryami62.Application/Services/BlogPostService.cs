#region

using suryami62.Application.Persistence;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Services;

public interface IBlogPostService
{
    Task<(List<BlogPost> Items, int Total)>
        GetPostsAsync(bool onlyPublished = true, int? skip = null, int? take = null, string? searchTerm = null);

    Task<BlogPost?> GetPostBySlugAsync(string slug);
    Task<BlogPost?> GetPostByIdAsync(int id);
    Task<BlogPost> CreatePostAsync(BlogPost post);
    Task UpdatePostAsync(BlogPost post);
    Task DeletePostAsync(int id);
}

public sealed class BlogPostService(IBlogPostRepository repository) : IBlogPostService
{
    public async Task<(List<BlogPost> Items, int Total)> GetPostsAsync(bool onlyPublished = true, int? skip = null,
        int? take = null, string? searchTerm = null)
    {
        return await repository.GetPostsAsync(onlyPublished, skip, take, searchTerm).ConfigureAwait(false);
    }

    public async Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        return await repository.GetBySlugAsync(slug).ConfigureAwait(false);
    }

    public async Task<BlogPost?> GetPostByIdAsync(int id)
    {
        return await repository.GetByIdAsync(id).ConfigureAwait(false);
    }

    public async Task<BlogPost> CreatePostAsync(BlogPost post)
    {
        return await repository.CreateAsync(post).ConfigureAwait(false);
    }

    public async Task UpdatePostAsync(BlogPost post)
    {
        await repository.UpdateAsync(post).ConfigureAwait(false);
    }

    public async Task DeletePostAsync(int id)
    {
        await repository.DeleteAsync(id).ConfigureAwait(false);
    }
}