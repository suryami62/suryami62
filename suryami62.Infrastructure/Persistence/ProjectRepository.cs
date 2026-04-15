// ============================================================================
// PROJECT REPOSITORY
// ============================================================================
// This class implements project/portfolio data access using Entity Framework Core.
// It stores and retrieves portfolio projects displayed on the Projects page.
//
// SIMILAR TO BLOG POST REPOSITORY:
// See BlogPostRepository.cs for detailed EF Core explanations.
// This file follows the same patterns with project-specific details.
//
// PROJECT ORDERING:
// Projects are ordered by DisplayOrder (not by date like blog posts).
// This allows featured projects to appear first regardless of creation date.
//
// UPDATE PATTERN:
// Uses EfRepositoryHelpers.UpdateExistingOrAttachModified() to handle
// EF Core tracking conflicts. See that helper for detailed explanation.
// ============================================================================

#region

using Microsoft.EntityFrameworkCore;
using suryami62.Application.Persistence;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Infrastructure.Persistence;

/// <summary>
///     Implements portfolio project data access using Entity Framework Core.
///     Handles database operations for the Projects table.
/// </summary>
public sealed class ProjectRepository : IProjectRepository
{
    // The database context - provides access to the Projects table
    private readonly ApplicationDbContext _context;

    /// <summary>
    ///     Creates a new project repository with the given database context.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    public ProjectRepository(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <summary>
    ///     Gets a paginated list of projects with total count.
    ///     Projects are ordered by DisplayOrder for featured-first arrangement.
    /// </summary>
    /// <param name="skip">Number of projects to skip (pagination).</param>
    /// <param name="take">Maximum projects to return (page size).</param>
    /// <returns>Tuple with (list of projects, total count).</returns>
    public async Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null)
    {
        // Step 1: Start with all projects, no tracking (read-only)
        var projectsQuery = _context.Projects.AsNoTracking();

        // Step 2: Count total projects (for pagination display)
        var total = await projectsQuery.CountAsync().ConfigureAwait(false);

        // Step 3: Load the paged/ordered project list
        var items = await LoadPagedProjectsAsync(projectsQuery, skip, take)
            .ConfigureAwait(false);

        // Step 4: Return both the items and total count
        return (items, total);
    }

    /// <summary>
    ///     Gets a single project by its database ID.
    /// </summary>
    /// <param name="id">The numeric database ID.</param>
    /// <returns>The project, or null if not found.</returns>
    public async Task<Project?> GetByIdAsync(int id)
    {
        // Query for project with matching ID
        // AsNoTracking() for read-only performance
        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id)
            .ConfigureAwait(false);

        return project;
    }

    /// <summary>
    ///     Creates a new project in the database.
    /// </summary>
    /// <param name="project">The project to create.</param>
    /// <returns>The created project (with assigned ID).</returns>
    public async Task<Project> CreateAsync(Project project)
    {
        // Step 1: Validate input
        ArgumentNullException.ThrowIfNull(project);

        // Step 2: Add to context (mark for insertion)
        _context.Projects.Add(project);

        // Step 3: Save to database (execute INSERT)
        await _context.SaveChangesAsync().ConfigureAwait(false);

        // Step 4: Return created project (now has ID)
        return project;
    }

    /// <summary>
    ///     Updates an existing project in the database.
    /// </summary>
    /// <param name="project">The project with updated values.</param>
    public async Task UpdateAsync(Project project)
    {
        // Step 1: Validate input
        ArgumentNullException.ThrowIfNull(project);

        // Step 2: Handle EF Core tracking conflict
        // See EfRepositoryHelpers.UpdateExistingOrAttachModified() for details
        EfRepositoryHelpers.UpdateExistingOrAttachModified(
            _context,
            _context.Projects,
            project,
            item => item.Id);

        // Step 3: Save changes to database (execute UPDATE)
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Deletes a project from the database.
    /// </summary>
    /// <param name="id">The ID of the project to delete.</param>
    public async Task DeleteAsync(int id)
    {
        // Step 1: Find the project by ID
        var project = await _context.Projects
            .FindAsync(id)
            .ConfigureAwait(false);

        // Step 2: If not found, nothing to delete
        if (project is null) return;

        // Step 3: Mark for deletion
        _context.Projects.Remove(project);

        // Step 4: Save changes (execute DELETE)
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Applies ordering and pagination to a project query.
    ///     Projects are ordered by DisplayOrder for featured-first arrangement.
    /// </summary>
    private static Task<List<Project>> LoadPagedProjectsAsync(
        IQueryable<Project> projectsQuery,
        int? skip,
        int? take)
    {
        // Step 1: Order by DisplayOrder (featured projects first)
        // Unlike blog posts (ordered by date), projects use manual ordering
        var orderedProjects = projectsQuery
            .OrderBy(project => project.DisplayOrder);

        // Step 2: Apply pagination (skip/take) if provided
        var pagedProjects = EfRepositoryHelpers
            .ApplyOptionalPaging(orderedProjects, skip, take);

        // Step 3: Execute query and return results
        return pagedProjects.ToListAsync();
    }
}