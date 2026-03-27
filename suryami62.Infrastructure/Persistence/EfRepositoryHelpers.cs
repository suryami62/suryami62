#region

using Microsoft.EntityFrameworkCore;

#endregion

namespace suryami62.Infrastructure.Persistence;

/// <summary>
///     Provides small EF Core helpers shared by repository implementations.
/// </summary>
internal static class EfRepositoryHelpers
{
    /// <summary>
    ///     Applies optional skip and take values only when they are positive.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being queried.</typeparam>
    /// <param name="query">The query to page.</param>
    /// <param name="skip">The optional number of items to skip.</param>
    /// <param name="take">The optional number of items to take.</param>
    /// <returns>The original or paged query.</returns>
    public static IQueryable<TEntity> ApplyOptionalPaging<TEntity>(
        IQueryable<TEntity> query,
        int? skip,
        int? take)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (skip is > 0) query = query.Skip(skip.Value);

        if (take is > 0) query = query.Take(take.Value);

        return query;
    }

    /// <summary>
    ///     Updates the currently tracked entity when one exists, or marks the supplied entity as modified otherwise.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being updated.</typeparam>
    /// <param name="context">The DbContext that tracks entity state.</param>
    /// <param name="set">The entity set used to inspect tracked instances.</param>
    /// <param name="entity">The detached or tracked entity with new values.</param>
    /// <param name="getId">Accessor used to compare entity identifiers.</param>
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

        var entityId = getId(entity);
        var trackedEntity = set.Local.FirstOrDefault(candidate => getId(candidate) == entityId);

        if (trackedEntity is not null && !ReferenceEquals(trackedEntity, entity))
        {
            // Reusing the tracked instance avoids EF tracking conflicts when callers send a detached entity copy.
            context.Entry(trackedEntity).CurrentValues.SetValues(entity);
            return;
        }

        if (trackedEntity is null) context.Entry(entity).State = EntityState.Modified;
    }
}