// ============================================================================
// JOURNEY HISTORY REPOSITORY
// ============================================================================
// This class implements journey/timeline data access using Entity Framework Core.
// It stores and retrieves career/education entries for the About page.
//
// WHAT IS DisplayOrder?
// DisplayOrder controls the sort position of items within a section.
// Lower numbers appear first. We auto-generate this so new items appear last.
//
// AUTO-INCREMENT LOGIC (GetNextDisplayOrderAsync):
// 1. Find the highest DisplayOrder in the target section
// 2. Add 1 to get the next position
// 3. Assign to the new item
//
// Example:
//   Existing items: Job A (order=1), Job B (order=2), Job C (order=3)
//   New item gets: order = 3 + 1 = 4 (appears last)
//
// QUERY PATTERN:
// - AsNoTracking(): Read-only query (faster, no change tracking)
// - Where(): Filter by section (Experience vs Education)
// - OrderBy(): Sort by DisplayOrder
// - ThenBy(): Secondary sort by Id (stable ordering if orders are equal)
// ============================================================================

#region

using Microsoft.EntityFrameworkCore;
using suryami62.Application.Persistence;
using suryami62.Data;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Infrastructure.Persistence;

/// <summary>
///     Implements journey history data access using Entity Framework Core.
///     Handles database operations for career/education timeline entries.
/// </summary>
public sealed class JourneyHistoryRepository : IJourneyHistoryRepository
{
    // The database context - provides access to the JourneyHistories table
    private readonly ApplicationDbContext _context;

    /// <summary>
    ///     Creates a new journey history repository with the given database context.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    public JourneyHistoryRepository(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <summary>
    ///     Gets all journey items for a specific section, ordered for display.
    ///     Results are sorted by DisplayOrder then by Id for consistent ordering.
    /// </summary>
    /// <param name="section">The section to filter by (Experience or Education).</param>
    /// <returns>List of journey items in display order.</returns>
    public Task<List<JourneyHistory>> GetBySectionAsync(JourneySection section)
    {
        // Build and execute the ordered query
        // ToListAsync() executes the SQL query and returns the results
        return GetOrderedSectionQuery(section).ToListAsync();
    }

    /// <summary>
    ///     Creates a new journey item with auto-assigned display order.
    ///     The item is placed at the end of its section automatically.
    /// </summary>
    /// <param name="item">The journey item to create.</param>
    /// <returns>The created item (with assigned ID and DisplayOrder).</returns>
    public async Task<JourneyHistory> CreateAsync(JourneyHistory item)
    {
        // Step 1: Validate input
        ArgumentNullException.ThrowIfNull(item);

        // Step 2: Calculate next display order for this section
        // This ensures the new item appears at the end of the timeline
        item.DisplayOrder = await GetNextDisplayOrderAsync(item.Section)
            .ConfigureAwait(false);

        // Step 3: Add to database context (mark for insertion)
        _context.JourneyHistories.Add(item);

        // Step 4: Save changes to database (execute INSERT)
        await _context.SaveChangesAsync().ConfigureAwait(false);

        // Step 5: Return the created item (now has assigned ID)
        return item;
    }

    /// <summary>
    ///     Deletes a journey item by its ID.
    ///     Silently succeeds if item doesn't exist (idempotent operation).
    /// </summary>
    /// <param name="id">The ID of the item to delete.</param>
    public async Task DeleteAsync(int id)
    {
        // Step 1: Find the item by ID
        // FindAsync checks memory cache first, then database (efficient)
        var item = await _context.JourneyHistories
            .FindAsync(id)
            .ConfigureAwait(false);

        // Step 2: If not found, nothing to delete (return silently)
        if (item is null) return;

        // Step 3: Mark item for deletion
        _context.JourneyHistories.Remove(item);

        // Step 4: Execute DELETE in database
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Builds a query for journey items in a section, ordered for display.
    ///     Uses AsNoTracking() for read-only performance optimization.
    /// </summary>
    private IQueryable<JourneyHistory> GetOrderedSectionQuery(JourneySection section)
    {
        // Start with all journey items
        return _context.JourneyHistories
            // AsNoTracking(): Read-only, skip change tracking (faster)
            .AsNoTracking()
            // Filter: Only items in the requested section
            .Where(item => item.Section == section)
            // Primary sort: By display order (timeline position)
            .OrderBy(item => item.DisplayOrder)
            // Secondary sort: By ID (stable ordering if DisplayOrder ties)
            .ThenBy(item => item.Id);
    }

    /// <summary>
    ///     Calculates the next DisplayOrder value for a section.
    ///     Finds the highest existing order and adds 1.
    /// </summary>
    private async Task<int> GetNextDisplayOrderAsync(JourneySection section)
    {
        // Step 1: Query for the maximum DisplayOrder in this section
        // (int?) casts to nullable int - MaxAsync returns null if no items exist
        var maxOrder = await _context.JourneyHistories
            .Where(existing => existing.Section == section)
            .Select(existing => (int?)existing.DisplayOrder)
            .MaxAsync()
            .ConfigureAwait(false);

        // Step 2: Calculate next order
        // If maxOrder is null (no items), use 0, then add 1
        // Result: First item gets order 1, second gets 2, etc.
        return (maxOrder ?? 0) + 1;
    }
}