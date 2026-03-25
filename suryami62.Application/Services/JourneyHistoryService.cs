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
///     Implements journey item operations by delegating to the configured repository.
/// </summary>
public sealed class JourneyHistoryService(IJourneyHistoryRepository repository) : IJourneyHistoryService
{
    /// <inheritdoc />
    public Task<List<JourneyHistory>> GetBySectionAsync(JourneySection section)
    {
        return repository.GetBySectionAsync(section);
    }

    /// <inheritdoc />
    public Task<JourneyHistory> CreateAsync(JourneyHistory item)
    {
        return repository.CreateAsync(item);
    }

    /// <inheritdoc />
    public Task DeleteAsync(int id)
    {
        return repository.DeleteAsync(id);
    }
}