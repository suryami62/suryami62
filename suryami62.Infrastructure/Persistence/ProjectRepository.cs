#region

using Microsoft.EntityFrameworkCore;
using suryami62.Application.Persistence;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Infrastructure.Persistence;

public sealed class ProjectRepository(ApplicationDbContext context) : IProjectRepository
{
    public async Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null)
    {
        var query = context.Projects.AsNoTracking().AsQueryable();
        var total = await query.CountAsync().ConfigureAwait(false);
        var orderedQuery = query.OrderBy(p => p.DisplayOrder);

        IQueryable<Project> itemsQuery = orderedQuery;
        if (skip.HasValue) itemsQuery = itemsQuery.Skip(skip.Value);

        if (take.HasValue) itemsQuery = itemsQuery.Take(take.Value);

        var items = await itemsQuery.ToListAsync().ConfigureAwait(false);
        return (items, total);
    }

    public async Task<Project?> GetByIdAsync(int id)
    {
        return await context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id).ConfigureAwait(false);
    }

    public async Task<Project> CreateAsync(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        context.Projects.Add(project);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return project;
    }

    public async Task UpdateAsync(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var tracked = context.Projects.Local.FirstOrDefault(p => p.Id == project.Id);
        if (tracked != null && !ReferenceEquals(tracked, project))
            context.Entry(tracked).CurrentValues.SetValues(project);
        else if (tracked == null) context.Entry(project).State = EntityState.Modified;

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id)
    {
        var project = await context.Projects.FindAsync(id).ConfigureAwait(false);
        if (project != null)
        {
            context.Projects.Remove(project);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}