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
        return await EntityServiceHelper
            .CreateAsync(context.Projects, context, project)
            .ConfigureAwait(false);
    }

    public async Task UpdateProjectAsync(Project project)
    {
        await EntityServiceHelper
            .UpdateAsync(context.Projects, context, project, current => current.Id)
            .ConfigureAwait(false);
    }

    public async Task DeleteProjectAsync(int id)
    {
        await EntityServiceHelper
            .DeleteAsync(context.Projects, context, id)
            .ConfigureAwait(false);
    }
}
