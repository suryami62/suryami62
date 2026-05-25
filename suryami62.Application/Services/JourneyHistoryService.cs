#region

using suryami62.Application.Persistence;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Services;

public interface IJourneyHistoryService
{
    Task<List<JourneyHistory>> GetBySectionAsync(JourneySection section);

    Task<JourneyHistory> CreateAsync(JourneyHistory item);

    Task DeleteAsync(int id);
}

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

    public async Task<List<JourneyHistory>> GetBySectionAsync(JourneySection section)
    {
        var cacheKey = $"{CacheKeyPrefix}section:{section}";

        if (_cacheService != null)
        {
            var cached = await _cacheService
                .GetAsync<List<JourneyHistory>>(cacheKey)
                .ConfigureAwait(false);

            if (cached != null)
                return cached;
        }

        if (_stampedeProtection != null && _cacheService != null)
        {
            var result = await _stampedeProtection
                .ExecuteAsync(cacheKey, async () =>
                {
                    var doubleCheck = await _cacheService
                        .GetAsync<List<JourneyHistory>>(cacheKey)
                        .ConfigureAwait(false);

                    if (doubleCheck != null) return doubleCheck;

                    var items = await _repository
                        .GetBySectionAsync(section)
                        .ConfigureAwait(false);

                    await _cacheService.SetAsync(cacheKey, items, CacheExpiration)
                        .ConfigureAwait(false);

                    return items;
                }).ConfigureAwait(false);

            return result;
        }

        var fallbackItems = await _repository
            .GetBySectionAsync(section)
            .ConfigureAwait(false);

        if (_cacheService != null)
            await _cacheService.SetAsync(cacheKey, fallbackItems, CacheExpiration)
                .ConfigureAwait(false);

        return fallbackItems;
    }

    public async Task<JourneyHistory> CreateAsync(JourneyHistory item)
    {
        var result = await _repository.CreateAsync(item)
            .ConfigureAwait(false);

        if (_cacheService != null)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}section:*")
                .ConfigureAwait(false);

        return result;
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id).ConfigureAwait(false);

        if (_cacheService != null)
            await _cacheService.RemoveByPatternAsync($"{CacheKeyPrefix}section:*")
                .ConfigureAwait(false);
    }
}