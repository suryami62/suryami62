#region

using suryami62.Domain.Models;

#endregion

namespace suryami62.Application.Persistence;

public interface IJourneyHistoryRepository
{
    Task<List<JourneyHistory>> GetBySectionAsync(
        JourneySection section,
        CancellationToken cancellationToken = default);

    Task<JourneyHistory> CreateAsync(JourneyHistory item, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}