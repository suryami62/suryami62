// ============================================================================
// EF CORE REPOSITORY HELPERS
// ============================================================================
// This file contains helper methods used by multiple EF Core repositories.
//
// WHAT IS EF CORE ENTITY TRACKING?
// EF Core keeps track of entities (objects) it has loaded from the database.
// This is called "change tracking". When you call SaveChanges(), EF Core
// knows what has been modified and generates appropriate SQL UPDATE statements.
//
// THE TRACKING PROBLEM:
// When updating an entity, EF Core can have conflicts if:
// 1. An entity with the same ID is already being tracked, AND
// 2. You try to attach a different object instance with the same ID
//
// EF Core throws: "The instance of entity type 'X' is already being tracked"
//
// SOLUTION (UpdateExistingOrAttachModified):
// Check if EF Core is already tracking an entity with the same ID.
// - If yes: Copy new values to the tracked instance (avoiding the conflict)
// - If no: Mark the new entity as modified (standard update)
//
// PAGINATION (ApplyOptionalPaging):
// Skip() and Take() are LINQ methods for pagination:
// - Skip(10): Skip first 10 results (for page 2)
// - Take(10): Return only 10 results (page size)
// ============================================================================

#region

using Microsoft.EntityFrameworkCore;

#endregion

namespace suryami62.Infrastructure.Persistence;

/// <summary>
///     Helper methods shared by EF Core repository implementations.
///     Provides pagination and entity tracking conflict resolution.
/// </summary>
internal static class EfRepositoryHelpers
{
    /// <summary>
    ///     Applies pagination (skip/take) to a query if values are provided.
    ///     Only applies Skip/Take if the value is positive (> 0).
    /// </summary>
    /// <typeparam name="TEntity">The type of entity being queried (e.g., BlogPost).</typeparam>
    /// <param name="query">The IQueryable to apply pagination to.</param>
    /// <param name="skip">Number of items to skip (for pagination). Null means no skipping.</param>
    /// <param name="take">Maximum number of items to return. Null means no limit.</param>
    /// <returns>The query with pagination applied (if parameters provided).</returns>
    public static IQueryable<TEntity> ApplyOptionalPaging<TEntity>(
        IQueryable<TEntity> query,
        int? skip,
        int? take)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Apply Skip if value is positive
        // Example: skip=10 means "skip first 10 results" (for page 2 with page size 10)
        if (skip is > 0) query = query.Skip(skip.Value);

        // Apply Take if value is positive
        // Example: take=10 means "return only 10 results" (page size of 10)
        if (take is > 0) query = query.Take(take.Value);

        return query;
    }

    /// <summary>
    ///     Safely updates an entity, handling EF Core entity tracking conflicts.
    ///     THE PROBLEM:
    ///     EF Core tracks entities by their ID. If you try to update an entity
    ///     using a different object instance than the one EF Core is tracking,
    ///     you get an error: "The instance of entity type 'X' is already being tracked"
    ///     THE SOLUTION:
    ///     1. Check if EF Core is already tracking an entity with the same ID
    ///     2. If tracked entity exists: Copy new values to the tracked instance
    ///     3. If no tracked entity: Mark the new entity as modified
    ///     This allows "detached" entities (loaded separately) to be used for updates.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being updated (e.g., BlogPost).</typeparam>
    /// <param name="context">The DbContext that manages entity tracking.</param>
    /// <param name="set">The DbSet for the entity type (e.g., context.BlogPosts).</param>
    /// <param name="entity">The entity with new values (may be detached from tracking).</param>
    /// <param name="getId">Function to get the entity's ID (e.g., e => e.Id).</param>
    public static void UpdateExistingOrAttachModified<TEntity>(
        DbContext context,
        DbSet<TEntity> set,
        TEntity entity,
        Func<TEntity, int> getId)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(getId);

        // Step 1: Get the ID of the entity we want to update
        var entityId = getId(entity);

        // Step 2: Check if EF Core is already tracking an entity with this ID
        // set.Local contains all entities currently being tracked by this DbSet
        var trackedEntity = set.Local
            .FirstOrDefault(candidate => getId(candidate) == entityId);

        // Step 3: Handle the tracking conflict scenario
        // If trackedEntity exists AND it's a different object instance than our entity
        if (trackedEntity is not null && !ReferenceEquals(trackedEntity, entity))
        {
            // EF Core is tracking a different instance with the same ID.
            // We cannot attach our entity because it would cause a conflict.
            // Solution: Copy our new values to the already-tracked instance.
            // EF Core will then detect changes and update the database.
            context.Entry(trackedEntity).CurrentValues.SetValues(entity);
            return;
        }

        // Step 4: No tracking conflict - safe to mark our entity as modified
        // This happens when:
        // - No entity with this ID is being tracked, OR
        // - The tracked entity IS the same object instance as our entity
        if (trackedEntity is null)
            // Not being tracked - explicitly mark as modified
            context.Entry(entity).State = EntityState.Modified;

        // If trackedEntity is not null but ReferenceEquals is true,
        // our entity IS the tracked entity, so EF Core already knows about it.
        // No action needed - EF Core will detect changes automatically.
    }
}