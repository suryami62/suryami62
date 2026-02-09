#region

using Microsoft.EntityFrameworkCore;
using suryami62.Data;
using suryami62.Data.Models;

#endregion

namespace suryami62.Services;

internal interface IProjectService
{
    Task<List<Project>> GetProjectsAsync();
    Task<Project?> GetProjectByIdAsync(int id);
    Task<Project> CreateProjectAsync(Project project);
    Task UpdateProjectAsync(Project project);
    Task DeleteProjectAsync(int id);
}

internal sealed class ProjectService(ApplicationDbContext context) : IProjectService
{
    public async Task<List<Project>> GetProjectsAsync()
    {
        return await context.Projects.OrderBy(p => p.DisplayOrder).ToListAsync().ConfigureAwait(false);
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        return await context.Projects.FindAsync(id).ConfigureAwait(false);
    }

    public async Task<Project> CreateProjectAsync(Project project)
    {
        context.Projects.Add(project);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return project;
    }

    public async Task UpdateProjectAsync(Project project)
    {
        var tracked = context.Projects.Local.FirstOrDefault(p => p.Id == project.Id);
        if (tracked != null && !ReferenceEquals(tracked, project))
            context.Entry(tracked).CurrentValues.SetValues(project);
        else if (tracked == null) context.Entry(project).State = EntityState.Modified;
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteProjectAsync(int id)
    {
        var project = await context.Projects.FindAsync(id).ConfigureAwait(false);
        if (project != null)
        {
            context.Projects.Remove(project);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}