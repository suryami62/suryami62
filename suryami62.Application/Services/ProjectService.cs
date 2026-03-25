#region

using suryami62.Application.Persistence;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Services;

/// <summary>
///     Defines application operations for managing portfolio projects.
/// </summary>
public interface IProjectService
{
    /// <summary>
    ///     Gets projects with optional paging information.
    /// </summary>
    /// <param name="skip">The optional number of items to skip.</param>
    /// <param name="take">The optional maximum number of items to return.</param>
    /// <returns>A tuple containing the matching items and total count.</returns>
    Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null);

    /// <summary>
    ///     Gets a project by its identifier.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <returns>The matching project when found; otherwise <see langword="null" />.</returns>
    Task<Project?> GetProjectByIdAsync(int id);

    /// <summary>
    ///     Creates a new project.
    /// </summary>
    /// <param name="project">The project to persist.</param>
    /// <returns>The created project.</returns>
    Task<Project> CreateProjectAsync(Project project);

    /// <summary>
    ///     Updates an existing project.
    /// </summary>
    /// <param name="project">The project to update.</param>
    Task UpdateProjectAsync(Project project);

    /// <summary>
    ///     Deletes a project by identifier.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    Task DeleteProjectAsync(int id);
}

/// <summary>
///     Implements project operations by delegating to the configured repository.
/// </summary>
public sealed class ProjectService(IProjectRepository repository) : IProjectService
{
    /// <inheritdoc />
    public Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null)
    {
        return repository.GetProjectsAsync(skip, take);
    }

    /// <inheritdoc />
    public Task<Project?> GetProjectByIdAsync(int id)
    {
        return repository.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public Task<Project> CreateProjectAsync(Project project)
    {
        return repository.CreateAsync(project);
    }

    /// <inheritdoc />
    public Task UpdateProjectAsync(Project project)
    {
        return repository.UpdateAsync(project);
    }

    /// <inheritdoc />
    public Task DeleteProjectAsync(int id)
    {
        return repository.DeleteAsync(id);
    }
}