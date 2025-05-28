using System;

namespace TemporaryName.Infrastructure.Caching.Abstractions.Settings;

public class CacheEntryOptions
{
    /// <summary>
    /// Gets or sets an absolute expiration date for the cache entry.
    /// If set, the entry will be removed from the cache at this time, regardless of activity.
    /// </summary>
    public DateTimeOffset? AbsoluteExpiration { get; set; }

    /// <summary>
    /// Gets or sets an absolute expiration time, relative to now.
    /// If set, this effectively defines the Time To Live (TTL) for the cache entry from the moment it's set.
    /// </summary>
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

    /// <summary>
    /// Gets or sets how long a cache entry can be inactive (e.g., not accessed) before it will be removed.
    /// If the entry is accessed, its lifetime is extended by this duration.
    /// This will not extend the entry lifetime beyond the AbsoluteExpiration, if set.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }

}
