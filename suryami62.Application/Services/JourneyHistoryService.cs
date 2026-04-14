// ============================================================================
// JOURNEY HISTORY SERVICE
// ============================================================================
// This service manages career timeline items displayed on the About page.
//
// WHAT IS JOURNEY HISTORY?
// Journey items represent milestones in your career:
// - Jobs and work experience
// - Education and degrees
// - Certifications and credentials
// - Major projects or achievements
//
// SECTIONS:
// - Professional: Work history, job positions
// - Certification: Courses, certificates, credentials
// - Other custom sections defined in JourneySection enum
//
// CACHING:
// Journey data changes infrequently (when you get a new job or certificate).
// We cache by section so the About page loads quickly without database hits.
//
// CACHE KEYS:
// - journey:section:Professional    - Work experience items
// - journey:section:Certification   - Certifications
// ============================================================================

#region

using suryami62.Application.Persistence;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Services;

/// <summary>
///     Interface for journey/timeline item operations.
///     Allows using fake implementations for testing.
/// </summary>
public interface IJourneyHistoryService
{
    /// <summary>
    ///     Gets all journey items for a specific section (e.g., Professional, Certification).
    ///     Results are ordered by date for timeline display.
    /// </summary>
    /// <param name="section">The section to retrieve (e.g., Professional).</param>
    /// <returns>List of journey items in that section.</returns>
    Task<List<JourneyHistory>> GetBySectionAsync(JourneySection section);

    /// <summary>
    ///     Creates a new journey item (e.g., a new job or certification).
    /// </summary>
    /// <param name="item">The journey item to create.</param>
    /// <returns>The created item (with assigned ID).</returns>
    Task<JourneyHistory> CreateAsync(JourneyHistory item);

    /// <summary>
    ///     Deletes a journey item by its ID.
    /// </summary>
    /// <param name="id">The ID of the item to delete.</param>
    Task DeleteAsync(int id);
}

/// <summary>
///     Implements journey operations with Redis caching and stampede protection.
///     Uses cache-aside pattern for optimal performance.
/// </summary>
public sealed class JourneyHistoryService : IJourneyHistoryService
{
    // All cache keys start with this prefix
    private const string CacheKeyPrefix = "journey:";

    // Cache entries expire after 15 minutes
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

    // Optional cache service (null if Redis not configured)
    private readonly IRedisCacheService? _cacheService;

    // Repository for database operations
    private readonly IJourneyHistoryRepository _repository;

    // Optional stampede protection (prevents database overload)
    private readonly CacheStampedeProtection? _stampedeProtection;

    /// <summary>
    ///     Creates a new journey history service.
    /// </summary>
    /// <param name="repository">The database repository.</param>
    /// <param name="cacheService">Optional Redis cache service.</param>
    /// <param name="stampedeProtection">Optional stampede protection locking.</param>
    public JourneyHistoryService(
        IJourneyHistoryRepository repository,
        IRedisCacheService? cacheService = null,
        CacheStampedeProtection? stampedeProtection = null)
    {
        _repository = repository;
        _cacheService = cacheService;
        _stampedeProtection = stampedeProtection;
    }

    /// <summary>
    ///     Gets journey items for a section with caching.
    /// </summary>
    public async Task<List<JourneyHistory>> GetBySectionAsync(JourneySection section)
    {
        // Step 1: Build cache key for this section
        // Example: "journey:section:Professional"
        var cacheKey = $"{CacheKeyPrefix}section:{section}";

        // Step 2: Try to get from cache first
        if (_cacheService != null)
        {
            var cached = await _cacheService
                .GetAsync<List<JourneyHistory>>(cacheKey)
                .ConfigureAwait(false);

            if (cached != null)
                // Cache hit - return immediately
                return cached;
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
                        .GetAsync<List<JourneyHistory>>(cacheKey)
                        .ConfigureAwait(false);

                    if (doubleCheck != null) return doubleCheck;

                    // Step 3b: Fetch from database
                    var items = await _repository
                        .GetBySectionAsync(section)
                        .ConfigureAwait(false);

                    // Step 3c: Store in cache
                    await _cacheService.SetAsync(cacheKey, items, CacheExpiration)
                        .ConfigureAwait(false);

                    return items;
                }).ConfigureAwait(false);

            return result;
        }

        // Step 4: No stampede protection - fetch directly
        var fallbackItems = await _repository
            .GetBySectionAsync(section)
            .ConfigureAwait(false);

        // Store in cache if available
        if (_cacheService != null)
            await _cacheService.SetAsync(cacheKey, fallbackItems, CacheExpiration)
                .ConfigureAwait(false);

        return fallbackItems;
    }

    /// <summary>
    ///     Creates a new journey item and invalidates section caches.
    /// </summary>
    public async Task<JourneyHistory> CreateAsync(JourneyHistory item)
    {
        // Save to database first
        var result = await _repository.CreateAsync(item)
            .ConfigureAwait(false);

        // Invalidate section caches (new item might belong to a section)
        if (_cacheService != null)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}section:*")
                .ConfigureAwait(false);

        return result;
    }

    /// <summary>
    ///     Deletes a journey item and invalidates section caches.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        // Delete from database
        await _repository.DeleteAsync(id).ConfigureAwait(false);

        // Invalidate section caches (item removed from display)
        if (_cacheService != null)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}section:*")
                .ConfigureAwait(false);
    }
}