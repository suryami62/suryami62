#region

using Microsoft.EntityFrameworkCore;
using suryami62.Application.Persistence;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Infrastructure.Persistence;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly ApplicationDbContext _context;

    public ProjectRepository(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<(List<Project> Items, int Total)> GetProjectsAsync(
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        var projectsQuery = _context.Projects.AsNoTracking();

        var total = await projectsQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await LoadPagedProjectsAsync(projectsQuery, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return (items, total);
    }

    public async Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return project;
    }

    public async Task<Project> CreateAsync(Project project, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        _context.Projects.Add(project);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return project;
    }

    public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        EfRepositoryHelpers.UpdateExistingOrAttachModified(
            _context,
            _context.Projects,
            project,
            item => item.Id);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .FindAsync(new object[] { id }, cancellationToken)
            .ConfigureAwait(false);

        if (project is null) return;

        _context.Projects.Remove(project);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static Task<List<Project>> LoadPagedProjectsAsync(
        IQueryable<Project> projectsQuery,
        int? skip,
        int? take,
        CancellationToken cancellationToken)
    {
        var orderedProjects = projectsQuery
            .OrderBy(project => project.DisplayOrder);

        var pagedProjects = EfRepositoryHelpers
            .ApplyOptionalPaging(orderedProjects, skip, take);

        return pagedProjects.ToListAsync(cancellationToken);
    }
}