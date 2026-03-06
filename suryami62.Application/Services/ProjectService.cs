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
    public Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null)
    {
        return repository.GetProjectsAsync(skip, take);
    }

    public Task<Project?> GetProjectByIdAsync(int id)
    {
        return repository.GetByIdAsync(id);
    }

    public Task<Project> CreateProjectAsync(Project project)
    {
        return repository.CreateAsync(project);
    }

    public Task UpdateProjectAsync(Project project)
    {
        return repository.UpdateAsync(project);
    }

    public Task DeleteProjectAsync(int id)
    {
        return repository.DeleteAsync(id);
    }
}