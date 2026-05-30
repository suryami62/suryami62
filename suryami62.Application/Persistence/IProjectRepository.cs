#region

using suryami62.Domain.Models;

#endregion

namespace suryami62.Application.Persistence;

public interface IProjectRepository
{
    Task<(List<Project> Items, int Total)> GetProjectsAsync(
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default);

    Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Project> CreateAsync(Project project, CancellationToken cancellationToken = default);

    Task UpdateAsync(Project project, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}