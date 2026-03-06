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

public sealed class JourneyHistoryService(IJourneyHistoryRepository repository) : IJourneyHistoryService
{
    public Task<List<JourneyHistory>> GetBySectionAsync(JourneySection section)
    {
        return repository.GetBySectionAsync(section);
    }

    public Task<JourneyHistory> CreateAsync(JourneyHistory item)
    {
        return repository.CreateAsync(item);
    }

    public Task DeleteAsync(int id)
    {
        return repository.DeleteAsync(id);
    }
}