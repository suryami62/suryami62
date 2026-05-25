#region

using suryami62.Application.Persistence;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Services;

public interface IProjectService
{
    Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null);

    Task<Project?> GetProjectByIdAsync(int id);

    Task<Project> CreateProjectAsync(Project project);

    Task UpdateProjectAsync(Project project);

    Task DeleteProjectAsync(int id);
}

public sealed class ProjectService : IProjectService
{
    private const string CacheKeyPrefix = "projects:";

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

    private readonly IRedisCacheService? _cacheService;

    private readonly IProjectRepository _repository;

    private readonly CacheStampedeProtection? _stampedeProtection;

    public ProjectService(
        IProjectRepository repository,
        IRedisCacheService? cacheService = null,
        CacheStampedeProtection? stampedeProtection = null)
    {
        _repository = repository;
        _cacheService = cacheService;
        _stampedeProtection = stampedeProtection;
    }

    public async Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null)
    {
        var cacheKey = $"{CacheKeyPrefix}list:{skip ?? 0}:{take ?? 0}";

        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<CachedProjectList>(cacheKey)
                .ConfigureAwait(false);

            if (cached != null)
                return (cached.Items, cached.Total);
        }

        if (_stampedeProtection != null && _cacheService != null)
        {
            var result = await _stampedeProtection
                .ExecuteAsync(cacheKey, async () =>
                {
                    var doubleCheck = await _cacheService
                        .GetAsync<CachedProjectList>(cacheKey)
                        .ConfigureAwait(false);

                    if (doubleCheck != null) return (doubleCheck.Items, doubleCheck.Total);

                    var dbResult = await _repository
                        .GetProjectsAsync(skip, take)
                        .ConfigureAwait(false);

                    await _cacheService.SetAsync(
                        cacheKey,
                        new CachedProjectList(dbResult.Items, dbResult.Total),
                        CacheExpiration).ConfigureAwait(false);

                    return dbResult;
                }).ConfigureAwait(false);

            return result;
        }

        var fallbackResult = await _repository
            .GetProjectsAsync(skip, take)
            .ConfigureAwait(false);

        if (_cacheService != null)
            await _cacheService.SetAsync(
                cacheKey,
                new CachedProjectList(fallbackResult.Items, fallbackResult.Total),
                CacheExpiration).ConfigureAwait(false);

        return fallbackResult;
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        var cacheKey = $"{CacheKeyPrefix}id:{id}";

        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<Project>(cacheKey).ConfigureAwait(false);
            if (cached != null) return cached;
        }

        if (_stampedeProtection != null && _cacheService != null)
        {
            var result = await _stampedeProtection
                .ExecuteAsync(cacheKey, async () =>
                {
                    var doubleCheck = await _cacheService.GetAsync<Project>(cacheKey)
                        .ConfigureAwait(false);
                    if (doubleCheck != null) return doubleCheck;

                    var project = await _repository.GetByIdAsync(id).ConfigureAwait(false);

                    if (project != null)
                        await _cacheService.SetAsync(cacheKey, project, CacheExpiration)
                            .ConfigureAwait(false);

                    return project;
                }).ConfigureAwait(false);

            return result;
        }

        var fallbackProject = await _repository.GetByIdAsync(id).ConfigureAwait(false);

        if (_cacheService != null && fallbackProject != null)
            await _cacheService.SetAsync(cacheKey, fallbackProject, CacheExpiration)
                .ConfigureAwait(false);

        return fallbackProject;
    }

    public async Task<Project> CreateProjectAsync(Project project)
    {
        var result = await _repository.CreateAsync(project).ConfigureAwait(false);

        if (_cacheService != null)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*")
                .ConfigureAwait(false);

        return result;
    }

    public async Task UpdateProjectAsync(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        await _repository.UpdateAsync(project).ConfigureAwait(false);

        if (_cacheService != null)
        {
            await _cacheService.RemoveEntryAsync($"{CacheKeyPrefix}id:{project.Id}")
                .ConfigureAwait(false);

            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*")
                .ConfigureAwait(false);
        }
    }

    public async Task DeleteProjectAsync(int id)
    {
        await _repository.DeleteAsync(id).ConfigureAwait(false);

        if (_cacheService != null)
        {
            await _cacheService.RemoveEntryAsync($"{CacheKeyPrefix}id:{id}")
                .ConfigureAwait(false);

            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*")
                .ConfigureAwait(false);
        }
    }

    private sealed record CachedProjectList(List<Project> Items, int Total);
}