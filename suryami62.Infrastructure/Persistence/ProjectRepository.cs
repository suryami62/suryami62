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

    public async Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null)
    {
        var projectsQuery = _context.Projects.AsNoTracking();

        var total = await projectsQuery.CountAsync().ConfigureAwait(false);

        var items = await LoadPagedProjectsAsync(projectsQuery, skip, take)
            .ConfigureAwait(false);

        return (items, total);
    }

    public async Task<Project?> GetByIdAsync(int id)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id)
            .ConfigureAwait(false);

        return project;
    }

    public async Task<Project> CreateAsync(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        _context.Projects.Add(project);

        await _context.SaveChangesAsync().ConfigureAwait(false);

        return project;
    }

    public async Task UpdateAsync(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        EfRepositoryHelpers.UpdateExistingOrAttachModified(
            _context,
            _context.Projects,
            project,
            item => item.Id);

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id)
    {
        var project = await _context.Projects
            .FindAsync(id)
            .ConfigureAwait(false);

        if (project is null) return;

        _context.Projects.Remove(project);

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    private static Task<List<Project>> LoadPagedProjectsAsync(
        IQueryable<Project> projectsQuery,
        int? skip,
        int? take)
    {
        var orderedProjects = projectsQuery
            .OrderBy(project => project.DisplayOrder);

        var pagedProjects = EfRepositoryHelpers
            .ApplyOptionalPaging(orderedProjects, skip, take);

        return pagedProjects.ToListAsync();
    }
}