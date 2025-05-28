using System;

namespace TemporaryName.Infrastructure.Caching.Abstractions;

/// <summary>
/// Defines a service for generating cache keys.
/// Centralizing key generation helps maintain consistency and avoid collisions.
/// </summary>
public interface ICacheKeyService
{
    /// <summary>
    /// Generates a cache key based on a base key and optional arguments.
    /// </summary>
    /// <param name="baseKey">The primary part of the cache key.</param>
    /// <param name="args">Optional arguments to be incorporated into the key, ensuring uniqueness.</param>
    /// <returns>A string representing the generated cache key.</returns>
    /// <remarks>
    /// Implementation should handle normalization and consistent formatting of arguments.
    /// For example, "user:123:profile" or "products:category:electronics".
    /// </remarks>
    string GenerateCacheKey(string prefix, params object[] args);
}

