#region

using Microsoft.EntityFrameworkCore;
using suryami62.Application.Persistence;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Infrastructure.Persistence;

/// <summary>
///     Persists and queries projects using Entity Framework Core.
/// </summary>
public sealed class ProjectRepository(ApplicationDbContext context) : IProjectRepository
{
    /// <inheritdoc />
    public async Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null)
    {
        var projectsQuery = context.Projects.AsNoTracking();
        var total = await projectsQuery.CountAsync().ConfigureAwait(false);
        var items = await LoadPagedProjectsAsync(projectsQuery, skip, take).ConfigureAwait(false);

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<Project?> GetByIdAsync(int id)
    {
        return await context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Project> CreateAsync(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        context.Projects.Add(project);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return project;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        EfRepositoryHelpers.UpdateExistingOrAttachModified(context, context.Projects, project, item => item.Id);

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var project = await context.Projects.FindAsync(id).ConfigureAwait(false);
        if (project is null) return;

        context.Projects.Remove(project);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    private static Task<List<Project>> LoadPagedProjectsAsync(
        IQueryable<Project> projectsQuery,
        int? skip,
        int? take)
    {
        var orderedProjects = projectsQuery.OrderBy(project => project.DisplayOrder);
        var pagedProjects = EfRepositoryHelpers.ApplyOptionalPaging(orderedProjects, skip, take);
        return pagedProjects.ToListAsync();
    }
}