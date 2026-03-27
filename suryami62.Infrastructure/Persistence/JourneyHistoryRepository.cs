#region

using Microsoft.EntityFrameworkCore;
using suryami62.Application.Persistence;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Infrastructure.Persistence;

/// <summary>
///     Persists and queries journey history items using Entity Framework Core.
/// </summary>
public sealed class JourneyHistoryRepository(ApplicationDbContext context) : IJourneyHistoryRepository
{
    /// <inheritdoc />
    public Task<List<JourneyHistory>> GetBySectionAsync(JourneySection section)
    {
        return GetOrderedSectionQuery(section).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<JourneyHistory> CreateAsync(JourneyHistory item)
    {
        ArgumentNullException.ThrowIfNull(item);

        item.DisplayOrder = await GetNextDisplayOrderAsync(item.Section).ConfigureAwait(false);

        context.JourneyHistories.Add(item);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return item;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var item = await context.JourneyHistories.FindAsync(id).ConfigureAwait(false);
        if (item is null) return;

        context.JourneyHistories.Remove(item);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    private IQueryable<JourneyHistory> GetOrderedSectionQuery(JourneySection section)
    {
        return context.JourneyHistories
            .AsNoTracking()
            .Where(item => item.Section == section)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Id);
    }

    private async Task<int> GetNextDisplayOrderAsync(JourneySection section)
    {
        var maxOrder = await context.JourneyHistories
            .Where(existing => existing.Section == section)
            .Select(existing => (int?)existing.DisplayOrder)
            .MaxAsync()
            .ConfigureAwait(false);

        return (maxOrder ?? 0) + 1;
    }
}