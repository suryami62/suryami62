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
    public Task<(List<BlogPost> Items, int Total)> GetPostsAsync(
        bool onlyPublished = true,
        int? skip = null,
        int? take = null,
        string? searchTerm = null)
    {
        return repository.GetPostsAsync(onlyPublished, skip, take, searchTerm);
    }

    public Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        return repository.GetBySlugAsync(slug);
    }

    public Task<BlogPost?> GetPostByIdAsync(int id)
    {
        return repository.GetByIdAsync(id);
    }

    public Task<BlogPost> CreatePostAsync(BlogPost post)
    {
        return repository.CreateAsync(post);
    }

    public Task UpdatePostAsync(BlogPost post)
    {
        return repository.UpdateAsync(post);
    }

    public Task DeletePostAsync(int id)
    {
        return repository.DeleteAsync(id);
    }
}