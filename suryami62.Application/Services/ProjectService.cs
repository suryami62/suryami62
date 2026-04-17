// ============================================================================
// PROJECT SERVICE
// ============================================================================
// This service manages portfolio projects with Redis caching for performance.
//
// WHAT IS A PORTFOLIO PROJECT?
// Projects showcase your work - websites, apps, libraries, tools you've built.
// Each project typically has:
// - Title and description
// - Image/screenshot
// - Links to live demo and source code
// - Technologies used
//
// CACHING STRATEGY:
// Same as BlogPostService - see that file for detailed comments on:
// - Cache-aside pattern
// - Stampede protection
// - Cache invalidation
//
// CACHE KEYS:
// - projects:list:0:10      - Paginated list of projects
// - projects:id:42            - Single project by ID
// ============================================================================

#region

using suryami62.Application.Persistence;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Services;

/// <summary>
///     Interface for portfolio project operations.
///     Allows using fake implementations for testing.
/// </summary>
public interface IProjectService
{
    /// <summary>
    ///     Gets a list of projects with optional pagination.
    /// </summary>
    /// <param name="skip">Number of projects to skip (for pagination).</param>
    /// <param name="take">Maximum projects to return (page size).</param>
    /// <returns>
    ///     A tuple containing:
    ///     - Items: The list of projects
    ///     - Total: Total number of projects (for pagination)
    /// </returns>
    Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null);

    /// <summary>
    ///     Gets a single project by its database ID.
    /// </summary>
    /// <param name="id">The numeric database ID.</param>
    /// <returns>The project, or null if not found.</returns>
    Task<Project?> GetProjectByIdAsync(int id);

    /// <summary>
    ///     Creates a new portfolio project.
    /// </summary>
    /// <param name="project">The project to create.</param>
    /// <returns>The created project (with assigned ID).</returns>
    Task<Project> CreateProjectAsync(Project project);

    /// <summary>
    ///     Updates an existing portfolio project.
    /// </summary>
    /// <param name="project">The project with updated values.</param>
    Task UpdateProjectAsync(Project project);

    /// <summary>
    ///     Deletes a portfolio project.
    /// </summary>
    /// <param name="id">The ID of the project to delete.</param>
    Task DeleteProjectAsync(int id);
}

/// <summary>
///     Implements project operations with Redis caching and stampede protection.
///     Uses cache-aside pattern for optimal performance.
/// </summary>
public sealed class ProjectService : IProjectService
{
    // All cache keys start with this prefix
    private const string CacheKeyPrefix = "projects:";

    // Cache entries expire after 15 minutes
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

    // Optional cache service (null if Redis not configured)
    private readonly IRedisCacheService? _cacheService;

    // Repository for database operations
    private readonly IProjectRepository _repository;

    // Optional stampede protection (prevents database overload)
    private readonly CacheStampedeProtection? _stampedeProtection;

    /// <summary>
    ///     Creates a new project service.
    /// </summary>
    /// <param name="repository">The database repository for projects.</param>
    /// <param name="cacheService">Optional Redis cache service.</param>
    /// <param name="stampedeProtection">Optional stampede protection locking.</param>
    public ProjectService(
        IProjectRepository repository,
        IRedisCacheService? cacheService = null,
        CacheStampedeProtection? stampedeProtection = null)
    {
        _repository = repository;
        _cacheService = cacheService;
        _stampedeProtection = stampedeProtection;
    }

    /// <summary>
    ///     Gets a list of projects with caching.
    /// </summary>
    public async Task<(List<Project> Items, int Total)> GetProjectsAsync(int? skip = null, int? take = null)
    {
        // Step 1: Build cache key for this query
        // Example: "projects:list:0:10"
        var cacheKey = $"{CacheKeyPrefix}list:{skip ?? 0}:{take ?? 0}";

        // Step 2: Try to get from cache first
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<CachedProjectList>(cacheKey)
                .ConfigureAwait(false);

            if (cached != null)
                // Cache hit - return immediately
                return (cached.Items, cached.Total);
        }

        // Step 3: Cache miss - need to fetch from database
        // Use stampede protection if available
        if (_stampedeProtection != null && _cacheService != null)
        {
            // Execute with locking - only one thread fetches from database
            var result = await _stampedeProtection
                .ExecuteAsync(cacheKey, async () =>
                {
                    // Step 3a: Double-check cache (another thread might have populated it)
                    var doubleCheck = await _cacheService
                        .GetAsync<CachedProjectList>(cacheKey)
                        .ConfigureAwait(false);

                    if (doubleCheck != null) return (doubleCheck.Items, doubleCheck.Total);

                    // Step 3b: Fetch from database
                    var dbResult = await _repository
                        .GetProjectsAsync(skip, take)
                        .ConfigureAwait(false);

                    // Step 3c: Store in cache
                    await _cacheService.SetAsync(
                        cacheKey,
                        new CachedProjectList(dbResult.Items, dbResult.Total),
                        CacheExpiration).ConfigureAwait(false);

                    return dbResult;
                }).ConfigureAwait(false);

            return result;
        }

        // Step 4: No stampede protection - fetch directly
        var fallbackResult = await _repository
            .GetProjectsAsync(skip, take)
            .ConfigureAwait(false);

        // Store in cache if available
        if (_cacheService != null)
            await _cacheService.SetAsync(
                cacheKey,
                new CachedProjectList(fallbackResult.Items, fallbackResult.Total),
                CacheExpiration).ConfigureAwait(false);

        return fallbackResult;
    }

    /// <summary>
    ///     Gets a single project by ID with caching.
    /// </summary>
    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        // Step 1: Build cache key
        // Example: "projects:id:42"
        var cacheKey = $"{CacheKeyPrefix}id:{id}";

        // Step 2: Try to get from cache first
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<Project>(cacheKey).ConfigureAwait(false);
            if (cached != null) return cached;
        }

        // Step 3: Cache miss - need to fetch from database
        // Use stampede protection if available
        if (_stampedeProtection != null && _cacheService != null)
        {
            // Execute with locking
            var result = await _stampedeProtection
                .ExecuteAsync(cacheKey, async () =>
                {
                    // Step 3a: Double-check cache
                    var doubleCheck = await _cacheService.GetAsync<Project>(cacheKey)
                        .ConfigureAwait(false);
                    if (doubleCheck != null) return doubleCheck;

                    // Step 3b: Fetch from database
                    var project = await _repository.GetByIdAsync(id).ConfigureAwait(false);

                    // Step 3c: Store in cache if found
                    if (project != null)
                        await _cacheService.SetAsync(cacheKey, project, CacheExpiration)
                            .ConfigureAwait(false);

                    return project;
                }).ConfigureAwait(false);

            return result;
        }

        // Step 4: No stampede protection - fetch directly
        var fallbackProject = await _repository.GetByIdAsync(id).ConfigureAwait(false);

        // Store in cache if available and found
        if (_cacheService != null && fallbackProject != null)
            await _cacheService.SetAsync(cacheKey, fallbackProject, CacheExpiration)
                .ConfigureAwait(false);

        return fallbackProject;
    }

    /// <summary>
    ///     Creates a new project and invalidates list caches.
    /// </summary>
    public async Task<Project> CreateProjectAsync(Project project)
    {
        // Save to database first
        var result = await _repository.CreateAsync(project).ConfigureAwait(false);

        // Invalidate list caches (new project should appear in lists)
        if (_cacheService != null)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*")
                .ConfigureAwait(false);

        return result;
    }

    /// <summary>
    ///     Updates an existing project and invalidates related caches.
    /// </summary>
    public async Task UpdateProjectAsync(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        // Update in database
        await _repository.UpdateAsync(project).ConfigureAwait(false);

        // Invalidate related caches
        if (_cacheService != null)
        {
            // Remove specific project cache
            await _cacheService.RemoveEntryAsync($"{CacheKeyPrefix}id:{project.Id}")
                .ConfigureAwait(false);

            // Remove all list caches (project data changed)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*")
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Deletes a project and invalidates related caches.
    /// </summary>
    public async Task DeleteProjectAsync(int id)
    {
        // Delete from database
        await _repository.DeleteAsync(id).ConfigureAwait(false);

        // Invalidate related caches
        if (_cacheService != null)
        {
            // Remove specific project cache
            await _cacheService.RemoveEntryAsync($"{CacheKeyPrefix}id:{id}")
                .ConfigureAwait(false);

            // Remove all list caches (project removed from lists)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}list:*")
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Helper class for caching paginated list results.
    ///     Stores both the items and total count needed for pagination.
    /// </summary>
    private sealed record CachedProjectList(List<Project> Items, int Total);
}