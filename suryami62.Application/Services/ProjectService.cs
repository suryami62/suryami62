#region

using suryami62.Application.Persistence;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Services;

/// <summary>
///     Defines application operations for managing portfolio projects.
/// </summary>
public interface IProjectService
{
    /// <summary>
    ///     Gets projects with optional paging information.
    /// </summary>
    /// <param name="skip">The optional number of items to skip.</param>
    /// <param name="take">The optional maximum number of items to return.</param>
    /// <returns>A tuple containing the matching items and total count.</returns>
    Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null);

    /// <summary>
    ///     Gets a project by its identifier.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <returns>The matching project when found; otherwise <see langword="null" />.</returns>
    Task<Project?> GetProjectByIdAsync(int id);

    /// <summary>
    ///     Creates a new project.
    /// </summary>
    /// <param name="project">The project to persist.</param>
    /// <returns>The created project.</returns>
    Task<Project> CreateProjectAsync(Project project);

    /// <summary>
    ///     Updates an existing project.
    /// </summary>
    /// <param name="project">The project to update.</param>
    Task UpdateProjectAsync(Project project);

    /// <summary>
    ///     Deletes a project by identifier.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    Task DeleteProjectAsync(int id);
}

/// <summary>
///     Implements project operations with Redis caching support.
///     Includes stampede protection to prevent multiple concurrent requests
///     from executing the same expensive operation when cache expires.
/// </summary>
/// <remarks>
///     Cache-aside pattern with stampede protection ensures optimal performance
///     during high traffic scenarios when cache entries expire.
///     See: https://learn.microsoft.com/aspnet/core/performance/caching/overview?view=aspnetcore-10.0#hybridcache
/// </remarks>
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

    /// <inheritdoc />
    public async Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null)
    {
        // Create cache key based on parameters
        var cacheKey = $"{CacheKeyPrefix}list:{skip ?? 0}:{take ?? 0}";

        // Try to get from cache first (cache-aside pattern)
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<CachedProjectList>(cacheKey).ConfigureAwait(false);
            if (cached != null) return (cached.Items, cached.Total);
        }

        // Cache miss - fetch from database with stampede protection
        // This prevents multiple concurrent requests from executing the same expensive operation
        if (_stampedeProtection != null && _cacheService != null)
        {
            var result = await _stampedeProtection.ExecuteAsync(cacheKey, async () =>
            {
                // Double-check cache after acquiring lock (another thread might have populated it)
                var doubleCheck = await _cacheService.GetAsync<CachedProjectList>(cacheKey).ConfigureAwait(false);
                if (doubleCheck != null) return (doubleCheck.Items, doubleCheck.Total);

                // Fetch from database
                var dbResult = await _repository.GetProjectsAsync(skip, take).ConfigureAwait(false);

                // Store in cache
                await _cacheService.SetAsync(cacheKey, new CachedProjectList(dbResult.Items, dbResult.Total),
                        CacheExpiration)
                    .ConfigureAwait(false);

                return dbResult;
            }).ConfigureAwait(false);

            return result;
        }

        // Fallback without stampede protection (when not configured)
        var fallbackResult = await _repository.GetProjectsAsync(skip, take).ConfigureAwait(false);

        if (_cacheService != null)
            await _cacheService.SetAsync(cacheKey, new CachedProjectList(fallbackResult.Items, fallbackResult.Total),
                    CacheExpiration)
                .ConfigureAwait(false);

        return fallbackResult;
    }

    /// <inheritdoc />
    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        var cacheKey = $"{CacheKeyPrefix}id:{id}";

        // Try to get from cache first
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<Project>(cacheKey).ConfigureAwait(false);
            if (cached != null) return cached;
        }

        // Cache miss - fetch from database with stampede protection
        if (_stampedeProtection != null && _cacheService != null)
            return await _stampedeProtection.ExecuteAsync(cacheKey, async () =>
            {
                // Double-check cache after acquiring lock
                var doubleCheck = await _cacheService.GetAsync<Project>(cacheKey).ConfigureAwait(false);
                if (doubleCheck != null) return doubleCheck;

                // Fetch from database
                var project = await _repository.GetByIdAsync(id).ConfigureAwait(false);

                // Store in cache if found
                if (project != null)
                    await _cacheService.SetAsync(cacheKey, project, CacheExpiration).ConfigureAwait(false);

                return project;
            }).ConfigureAwait(false);

        // Fallback without stampede protection (when not configured)
        var fallbackProject = await _repository.GetByIdAsync(id).ConfigureAwait(false);

        if (_cacheService != null && fallbackProject != null)
            await _cacheService.SetAsync(cacheKey, fallbackProject, CacheExpiration).ConfigureAwait(false);

        return fallbackProject;
    }

    /// <inheritdoc />
    public async Task<Project> CreateProjectAsync(Project project)
    {
        var result = await _repository.CreateAsync(project).ConfigureAwait(false);

        // Invalidate list caches
        if (_cacheService != null)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*").ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async Task UpdateProjectAsync(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        await _repository.UpdateAsync(project).ConfigureAwait(false);

        // Invalidate related caches
        if (_cacheService != null)
        {
            await _cacheService.RemoveEntryAsync($"{CacheKeyPrefix}id:{project.Id}").ConfigureAwait(false);
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*").ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task DeleteProjectAsync(int id)
    {
        await _repository.DeleteAsync(id).ConfigureAwait(false);

        // Invalidate caches
        if (_cacheService != null)
        {
            await _cacheService.RemoveEntryAsync($"{CacheKeyPrefix}id:{id}").ConfigureAwait(false);
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*").ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Internal type for caching paginated lists.
    /// </summary>
    private sealed record CachedProjectList(List<Project> Items, int Total);
}