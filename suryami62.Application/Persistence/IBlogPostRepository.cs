#region

using suryami62.Domain.Models;

#endregion

namespace suryami62.Application.Persistence;

public interface IBlogPostRepository
{
    Task<(List<BlogPost> Items, int Total)> GetPostsAsync(
        bool onlyPublished = true,
        int? skip = null,
        int? take = null,
        string? searchTerm = null);

    Task<BlogPost?> GetBySlugAsync(string slug);

    Task<bool> SlugExistsAsync(string slug, int? excludeId = null);

    Task<BlogPost?> GetByIdAsync(int id);

    Task<BlogPost> CreateAsync(BlogPost post);

    Task UpdateAsync(BlogPost post);

    Task DeleteAsync(int id);
}