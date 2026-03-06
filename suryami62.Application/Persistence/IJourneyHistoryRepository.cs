#region

using suryami62.Domain.Models;

#endregion

namespace suryami62.Application.Persistence;

public interface IJourneyHistoryRepository
{
    Task<List<JourneyHistory>> GetBySectionAsync(JourneySection section);
    Task<JourneyHistory> CreateAsync(JourneyHistory item);
    Task DeleteAsync(int id);
}