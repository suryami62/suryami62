#region

using suryami62.Application.Persistence;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Services;

public interface IProjectService
{
    Task<(List<Project> Items, int Total)> GetProjectsAsync(
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default);

    Task<Project?> GetProjectByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Project> CreateProjectAsync(Project project, CancellationToken cancellationToken = default);

    Task UpdateProjectAsync(Project project, CancellationToken cancellationToken = default);

    Task DeleteProjectAsync(int id, CancellationToken cancellationToken = default);
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

    public async Task<(List<Project> Items, int Total)> GetProjectsAsync(
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}list:{skip ?? 0}:{take ?? 0}";

        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<CachedProjectList>(cacheKey, cancellationToken)
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
                        .GetAsync<CachedProjectList>(cacheKey, cancellationToken)
                        .ConfigureAwait(false);

                    if (doubleCheck != null) return (doubleCheck.Items, doubleCheck.Total);

                    var dbResult = await _repository
                        .GetProjectsAsync(skip, take, cancellationToken)
                        .ConfigureAwait(false);

                    await _cacheService.SetAsync(
                        cacheKey,
                        new CachedProjectList(dbResult.Items, dbResult.Total),
                        CacheExpiration,
                        cancellationToken).ConfigureAwait(false);

                    return dbResult;
                }, cancellationToken).ConfigureAwait(false);

            return result;
        }

        var fallbackResult = await _repository
            .GetProjectsAsync(skip, take, cancellationToken)
            .ConfigureAwait(false);

        if (_cacheService != null)
            await _cacheService.SetAsync(
                cacheKey,
                new CachedProjectList(fallbackResult.Items, fallbackResult.Total),
                CacheExpiration,
                cancellationToken).ConfigureAwait(false);

        return fallbackResult;
    }

    public async Task<Project?> GetProjectByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}id:{id}";

        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<Project>(cacheKey, cancellationToken).ConfigureAwait(false);
            if (cached != null) return cached;
        }

        if (_stampedeProtection != null && _cacheService != null)
        {
            var result = await _stampedeProtection
                .ExecuteAsync(cacheKey, async () =>
                {
                    var doubleCheck = await _cacheService.GetAsync<Project>(cacheKey, cancellationToken)
                        .ConfigureAwait(false);
                    if (doubleCheck != null) return doubleCheck;

                    var project = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

                    if (project != null)
                        await _cacheService.SetAsync(cacheKey, project, CacheExpiration, cancellationToken)
                            .ConfigureAwait(false);

                    return project;
                }, cancellationToken).ConfigureAwait(false);

            return result;
        }

        var fallbackProject = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (_cacheService != null && fallbackProject != null)
            await _cacheService.SetAsync(cacheKey, fallbackProject, CacheExpiration, cancellationToken)
                .ConfigureAwait(false);

        return fallbackProject;
    }

    public async Task<Project> CreateProjectAsync(
        Project project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        NormalizeProjectTags(project);

        var result = await _repository.CreateAsync(project, cancellationToken).ConfigureAwait(false);

        if (_cacheService != null)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*", cancellationToken)
                .ConfigureAwait(false);

        return result;
    }

    public async Task UpdateProjectAsync(Project project, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        NormalizeProjectTags(project);

        await _repository.UpdateAsync(project, cancellationToken).ConfigureAwait(false);

        if (_cacheService != null)
        {
            await _cacheService.RemoveEntryAsync($"{CacheKeyPrefix}id:{project.Id}", cancellationToken)
                .ConfigureAwait(false);

            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*", cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public async Task DeleteProjectAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);

        if (_cacheService != null)
        {
            await _cacheService.RemoveEntryAsync($"{CacheKeyPrefix}id:{id}", cancellationToken)
                .ConfigureAwait(false);

            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*", cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private sealed record CachedProjectList(List<Project> Items, int Total);

    private static void NormalizeProjectTags(Project project)
    {
        var validationResult = ProjectTagFormatter.Validate(project.Tags);
        if (validationResult is not null)
        {
            throw new ArgumentException(
                validationResult.ErrorMessage ?? "Project tags are invalid.",
                nameof(project));
        }

        project.Tags = ProjectTagFormatter.Format(project.Tags);
    }
}