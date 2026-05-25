#region

using Microsoft.EntityFrameworkCore;

#endregion

namespace suryami62.Infrastructure.Persistence;

internal static class EfRepositoryHelpers
{
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

        var trackedEntity = set.Local
            .FirstOrDefault(candidate => getId(candidate) == entityId);

        if (trackedEntity is not null && !ReferenceEquals(trackedEntity, entity))
        {
            context.Entry(trackedEntity).CurrentValues.SetValues(entity);
            return;
        }

        if (trackedEntity is null)
            context.Entry(entity).State = EntityState.Modified;
    }
}