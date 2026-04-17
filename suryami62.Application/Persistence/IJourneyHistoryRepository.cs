// ============================================================================
// JOURNEY HISTORY REPOSITORY INTERFACE
// ============================================================================
// This interface defines the contract for journey/timeline data access.
//
// WHAT IS JOURNEY HISTORY?
// Journey items represent career milestones - jobs, education, certifications,
// displayed on a timeline (like LinkedIn experience section).
//
// SECTIONS:
// - Professional: Work experience, jobs, careers
// - Certification: Courses, certificates, credentials
//
// See JourneySection enum in Domain for all section types.
// ============================================================================

#region

using suryami62.Domain.Models;

#endregion

namespace suryami62.Application.Persistence;

/// <summary>
///     Defines operations for storing and retrieving journey/timeline items.
/// </summary>
public interface IJourneyHistoryRepository
{
    /// <summary>
    ///     Gets journey items for a specific section (e.g., Professional, Certification).
    ///     Results are ordered by date for timeline display.
    /// </summary>
    /// <param name="section">The section to filter by (e.g., Professional).</param>
    /// <returns>List of journey items in that section.</returns>
    Task<List<JourneyHistory>> GetBySectionAsync(JourneySection section);

    /// <summary>
    ///     Creates a new journey item in the database.
    /// </summary>
    /// <param name="item">The journey item to create.</param>
    /// <returns>The created item (with assigned ID).</returns>
    Task<JourneyHistory> CreateAsync(JourneyHistory item);

    /// <summary>
    ///     Deletes a journey item from the database.
    /// </summary>
    /// <param name="id">The ID of the item to delete.</param>
    Task DeleteAsync(int id);
}