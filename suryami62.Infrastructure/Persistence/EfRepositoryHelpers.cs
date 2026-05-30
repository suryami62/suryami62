#region

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using suryami62.Domain.Models;

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
            var trackedEntry = context.Entry(trackedEntity);

            trackedEntry.CurrentValues.SetValues(entity);
            ApplyOriginalConcurrencyVersion(trackedEntry, entity);
            return;
        }

        if (trackedEntity is null)
        {
            var entry = context.Entry(entity);

            entry.State = EntityState.Modified;
            ApplyOriginalConcurrencyVersion(entry, entity);
        }
    }

    private static void ApplyOriginalConcurrencyVersion<TEntity>(
        EntityEntry<TEntity> entry,
        TEntity entity)
        where TEntity : class
    {
        if (entity is not IConcurrencyTrackedEntity concurrencyTrackedEntity) return;

        var versionProperty = entry.Property(nameof(IConcurrencyTrackedEntity.Version));

        versionProperty.OriginalValue = concurrencyTrackedEntity.Version;
        versionProperty.IsModified = false;
    }
}