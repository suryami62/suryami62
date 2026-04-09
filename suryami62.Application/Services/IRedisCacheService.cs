#pragma warning disable CA1068 // CancellationToken parameters must come last

namespace suryami62.Services;

/// <summary>
///     Defines Redis-backed distributed caching operations.
/// </summary>
public interface IRedisCacheService
{
    /// <summary>
    ///     Gets a cached value by key.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>The cached value when found; otherwise <see langword="default" />.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    ///     Sets a value in the cache with optional expiration.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">Optional expiration time.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    ///     Removes a value from the cache by key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    Task RemoveEntryAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes multiple values from the cache by pattern.
    /// </summary>
    /// <param name="pattern">The key pattern to match (e.g., "posts:*").</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}