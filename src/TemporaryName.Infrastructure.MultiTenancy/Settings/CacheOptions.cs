using System;

namespace TemporaryName.Infrastructure.MultiTenancy.Settings;

public class CacheOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether tenant information caching is enabled.
    /// Defaults to true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the absolute expiration time for cached tenant entries, in seconds.
    /// After this time, the entry is considered stale and will be re-fetched.
    /// Defaults to 300 seconds (5 minutes).
    /// </summary>
    public int AbsoluteExpirationSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the sliding expiration time for cached tenant entries, in seconds.
    /// If an entry is accessed within this time, its expiration is renewed.
    /// Defaults to 60 seconds (1 minute).
    /// </summary>
    public int SlidingExpirationSeconds { get; set; } = 60;
}
