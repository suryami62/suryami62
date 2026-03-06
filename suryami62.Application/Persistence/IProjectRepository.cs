#region

using suryami62.Domain.Models;

#endregion

namespace suryami62.Application.Persistence;

/// <summary>
///     Provides persistence operations for <see cref="Project" /> entities.
/// </summary>
public interface IProjectRepository
{
    /// <summary>
    ///     Gets a page of projects and the total matching count.
    /// </summary>
    Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null);

    /// <summary>
    ///     Gets a project by its identifier.
    /// </summary>
    Task<Project?> GetByIdAsync(int id);

    /// <summary>
    ///     Creates a project.
    /// </summary>
    Task<Project> CreateAsync(Project project);

    /// <summary>
    ///     Updates a project.
    /// </summary>
    Task UpdateAsync(Project project);

    /// <summary>
    ///     Deletes a project.
    /// </summary>
    Task DeleteAsync(int id);
}