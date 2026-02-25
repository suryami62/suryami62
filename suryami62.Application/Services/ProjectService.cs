#region

using suryami62.Application.Persistence;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Services;

public interface IProjectService
{
    Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null);
    Task<Project?> GetProjectByIdAsync(int id);
    Task<Project> CreateProjectAsync(Project project);
    Task UpdateProjectAsync(Project project);
    Task DeleteProjectAsync(int id);
}

public sealed class ProjectService(IProjectRepository repository) : IProjectService
{
    public async Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null)
    {
        return await repository.GetProjectsAsync(skip, take).ConfigureAwait(false);
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        return await repository.GetByIdAsync(id).ConfigureAwait(false);
    }

    public async Task<Project> CreateProjectAsync(Project project)
    {
        return await repository.CreateAsync(project).ConfigureAwait(false);
    }

    public async Task UpdateProjectAsync(Project project)
    {
        await repository.UpdateAsync(project).ConfigureAwait(false);
    }

    public async Task DeleteProjectAsync(int id)
    {
        await repository.DeleteAsync(id).ConfigureAwait(false);
    }
}