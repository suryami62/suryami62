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
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, int? excludeId = null, CancellationToken cancellationToken = default);

    Task<BlogPost?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<BlogPost> CreateAsync(BlogPost post, CancellationToken cancellationToken = default);

    Task UpdateAsync(BlogPost post, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}