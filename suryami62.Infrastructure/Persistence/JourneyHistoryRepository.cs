#region

using Microsoft.EntityFrameworkCore;
using suryami62.Application.Persistence;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Infrastructure.Persistence;

public sealed class JourneyHistoryRepository : IJourneyHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public JourneyHistoryRepository(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public Task<List<JourneyHistory>> GetBySectionAsync(JourneySection section)
    {
        return GetOrderedSectionQuery(section).ToListAsync();
    }

    public async Task<JourneyHistory> CreateAsync(JourneyHistory item)
    {
        ArgumentNullException.ThrowIfNull(item);

        item.DisplayOrder = await GetNextDisplayOrderAsync(item.Section)
            .ConfigureAwait(false);

        _context.JourneyHistories.Add(item);

        await _context.SaveChangesAsync().ConfigureAwait(false);

        return item;
    }

    public async Task DeleteAsync(int id)
    {
        var item = await _context.JourneyHistories
            .FindAsync(id)
            .ConfigureAwait(false);

        if (item is null) return;

        _context.JourneyHistories.Remove(item);

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    private IQueryable<JourneyHistory> GetOrderedSectionQuery(JourneySection section)
    {
        return _context.JourneyHistories
            .AsNoTracking()
            .Where(item => item.Section == section)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Id);
    }

    private async Task<int> GetNextDisplayOrderAsync(JourneySection section)
    {
        var maxOrder = await _context.JourneyHistories
            .Where(existing => existing.Section == section)
            .Select(existing => (int?)existing.DisplayOrder)
            .MaxAsync()
            .ConfigureAwait(false);

        return (maxOrder ?? 0) + 1;
    }
}