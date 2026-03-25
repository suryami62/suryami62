#region

using suryami62.Domain.Models;

#endregion

namespace suryami62.Application.Persistence;

/// <summary>
///     Provides persistence operations for <see cref="JourneyHistory" /> entities.
/// </summary>
public interface IJourneyHistoryRepository
{
    /// <summary>
    ///     Gets journey items by section ordered for display.
    /// </summary>
    /// <param name="section">The section to filter by.</param>
    /// <returns>The matching journey items.</returns>
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
    /// <param name="id">The identifier to remove.</param>
    Task DeleteAsync(int id);
}