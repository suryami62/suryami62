#region

using suryami62.Domain.Models;

#endregion

namespace suryami62.Application.Persistence;

public interface IProjectRepository
{
    Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null);

    Task<Project?> GetByIdAsync(int id);

    Task<Project> CreateAsync(Project project);

    Task UpdateAsync(Project project);

    Task DeleteAsync(int id);
}