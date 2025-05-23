using System;
using TemporaryName.Infrastructure.Caching.Abstractions.Settings;

namespace TemporaryName.Infrastructure.Caching.Abstractions;

/// <summary>
/// Provides an abstraction for caching services.
/// This interface allows for different caching implementations (e.g., Redis, InMemory, Memcached)
/// to be used interchangeably.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves an item from the cache.
    /// </summary>
    /// <typeparam name="T">The type of the cached item.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>The cached item, or null if the key is not found or the item is of a different type.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or overwrites an item in the cache.
    /// </summary>
    /// <typeparam name="T">The type of the item to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The item to cache.</param>
    /// <param name="options">Optional <see cref="CacheEntryOptions"/> for this entry.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an item from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an item exists in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>True if the item exists, false otherwise.</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cache entries whose keys start with the specified prefix.
    /// Warning: This can be a slow operation on some cache providers (like Redis KEYS command in naive implementations).
    /// Prefer more specific removal if possible, or ensure the provider supports efficient prefix deletion (e.g., using SCAN).
    /// </summary>
    /// <param name="prefix">The prefix to match.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an item from the cache. If the item is not found, it is created using the <paramref name="factory"/>,
    /// added to the cache, and then returned.
    /// This is an atomic "get-or-add" operation if supported by the underlying cache provider,
    /// or a best-effort non-atomic version otherwise.
    /// </summary>
    /// <typeparam name="T">The type of the item to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">A factory function to create the item if it's not found in the cache.</param>
    /// <param name="options">Optional <see cref="CacheEntryOptions"/> for the new entry if created.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>The cached item.</returns>
    Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T?>> factory, // Changed to Func<Task<T?>> to allow async factory and nullable T
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);
}
