// ============================================================================
// PROJECT REPOSITORY INTERFACE
// ============================================================================
// This interface defines the contract for portfolio project data access.
//
// WHAT IS A PROJECT?
// Projects are portfolio items showcasing your work - websites, apps, libraries.
// Each project has a title, description, image, and links to demo/code.
//
// RETURNS TUPLE:
// GetProjectsAsync returns (List<Project> Items, int Total):
// - Items: The projects for this page
// - Total: Total count (for pagination)
// ============================================================================

#region

using suryami62.Domain.Models;

#endregion

namespace suryami62.Application.Persistence;

/// <summary>
///     Defines operations for storing and retrieving portfolio projects.
/// </summary>
public interface IProjectRepository
{
    /// <summary>
    ///     Gets a list of projects with optional pagination.
    /// </summary>
    /// <param name="skip">Number of projects to skip (for pagination).</param>
    /// <param name="take">Maximum number of projects to return (page size).</param>
    /// <returns>
    ///     A tuple containing:
    ///     - Items: The list of projects
    ///     - Total: Total number of projects (for pagination)
    /// </returns>
    Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null);

    /// <summary>
    ///     Gets a single project by its database ID.
    /// </summary>
    /// <param name="id">The numeric database ID.</param>
    /// <returns>The project, or null if not found.</returns>
    Task<Project?> GetByIdAsync(int id);

    /// <summary>
    ///     Creates a new project in the database.
    /// </summary>
    /// <param name="project">The project to create.</param>
    /// <returns>The created project (with assigned ID).</returns>
    Task<Project> CreateAsync(Project project);

    /// <summary>
    ///     Updates an existing project in the database.
    /// </summary>
    /// <param name="project">The project with updated values.</param>
    Task UpdateAsync(Project project);

    /// <summary>
    ///     Deletes a project from the database.
    /// </summary>
    /// <param name="id">The ID of the project to delete.</param>
    Task DeleteAsync(int id);
}