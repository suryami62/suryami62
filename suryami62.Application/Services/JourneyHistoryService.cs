#region

using suryami62.Application.Persistence;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Services;

/// <summary>
///     Defines application operations for managing journey timeline items.
/// </summary>
public interface IJourneyHistoryService
{
    /// <summary>
    ///     Gets journey items for a specific section.
    /// </summary>
    /// <param name="section">The section to filter by.</param>
    /// <returns>The ordered items in the section.</returns>
    Task<List<JourneyHistory>> GetBySectionAsync(JourneySection section);

    /// <summary>
    ///     Creates a new journey item.
    /// </summary>
    /// <param name="item">The item to persist.</param>
    /// <returns>The created item.</returns>
    Task<JourneyHistory> CreateAsync(JourneyHistory item);

    /// <summary>
    ///     Deletes a journey item by identifier.
    /// </summary>
    /// <param name="id">The identifier of the item to remove.</param>
    Task DeleteAsync(int id);
}

/// <summary>
///     Implements journey item operations with Redis caching support.
///     Includes stampede protection to prevent multiple concurrent requests
///     from executing the same expensive operation when cache expires.
/// </summary>
/// <remarks>
///     Cache-aside pattern with stampede protection ensures optimal performance
///     during high traffic scenarios when cache entries expire.
///     See: https://learn.microsoft.com/aspnet/core/performance/caching/overview?view=aspnetcore-10.0#hybridcache
/// </remarks>
public sealed class JourneyHistoryService : IJourneyHistoryService
{
    private const string CacheKeyPrefix = "journey:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);
    private readonly IRedisCacheService? _cacheService;

    private readonly IJourneyHistoryRepository _repository;
    private readonly CacheStampedeProtection? _stampedeProtection;

    public JourneyHistoryService(
        IJourneyHistoryRepository repository,
        IRedisCacheService? cacheService = null,
        CacheStampedeProtection? stampedeProtection = null)
    {
        _repository = repository;
        _cacheService = cacheService;
        _stampedeProtection = stampedeProtection;
    }

    /// <inheritdoc />
    public async Task<List<JourneyHistory>> GetBySectionAsync(JourneySection section)
    {
        var cacheKey = $"{CacheKeyPrefix}section:{section}";

        // Try to get from cache first (cache-aside pattern)
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<List<JourneyHistory>>(cacheKey).ConfigureAwait(false);
            if (cached != null) return cached;
        }

        // Cache miss - fetch from database with stampede protection
        // This prevents multiple concurrent requests from executing the same expensive operation
        if (_stampedeProtection != null && _cacheService != null)
            return await _stampedeProtection.ExecuteAsync(cacheKey, async () =>
            {
                // Double-check cache after acquiring lock (another thread might have populated it)
                var doubleCheck = await _cacheService.GetAsync<List<JourneyHistory>>(cacheKey).ConfigureAwait(false);
                if (doubleCheck != null) return doubleCheck;

                // Fetch from database
                var items = await _repository.GetBySectionAsync(section).ConfigureAwait(false);

                // Store in cache
                await _cacheService.SetAsync(cacheKey, items, CacheExpiration).ConfigureAwait(false);

                return items;
            }).ConfigureAwait(false);

        // Fallback without stampede protection (when not configured)
        var fallbackItems = await _repository.GetBySectionAsync(section).ConfigureAwait(false);

        if (_cacheService != null)
            await _cacheService.SetAsync(cacheKey, fallbackItems, CacheExpiration).ConfigureAwait(false);

        return fallbackItems;
    }

    /// <inheritdoc />
    public async Task<JourneyHistory> CreateAsync(JourneyHistory item)
    {
        var result = await _repository.CreateAsync(item).ConfigureAwait(false);

        // Invalidate section caches
        if (_cacheService != null)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}section:*").ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id).ConfigureAwait(false);

        // Invalidate section caches
        if (_cacheService != null)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}section:*").ConfigureAwait(false);
    }
}