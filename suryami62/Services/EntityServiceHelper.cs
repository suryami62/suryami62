#region

using Microsoft.EntityFrameworkCore;

#endregion

namespace suryami62.Services;

internal static class EntityServiceHelper
{
    public static async Task<T> CreateAsync<T>(DbSet<T> set, DbContext context, T entity)
        where T : class
    {
        set.Add(entity);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return entity;
    }

    public static async Task UpdateAsync<T, TKey>(
        DbSet<T> set,
        DbContext context,
        T entity,
        Func<T, TKey> getId)
        where T : class
        where TKey : notnull
    {
        var id = getId(entity);
        var tracked = set.Local.FirstOrDefault(item => EqualityComparer<TKey>.Default.Equals(getId(item), id));
        if (tracked != null && !ReferenceEquals(tracked, entity))
            context.Entry(tracked).CurrentValues.SetValues(entity);
        else if (tracked == null) context.Entry(entity).State = EntityState.Modified;
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task DeleteAsync<T, TKey>(DbSet<T> set, DbContext context, TKey id)
        where T : class
        where TKey : notnull
    {
        var entity = await set.FindAsync(id).ConfigureAwait(false);
        if (entity != null)
        {
            set.Remove(entity);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
