using System;

namespace TemporaryName.Infrastructure.Caching.Abstractions.Settings;

public class CachingProvidersOptions
{
    public const string SectionName = "CachingProviders";

    /// <summary>
    /// A list of cache provider keys (e.g., "Redis", "Memcached", "Garnet") that should be activated.
    /// These keys should match the keys used for keyed DI registration.
    /// </summary>
    public List<string> ActiveProviders { get; set; } = new();

    /// <summary>
    /// The key of the default cache provider to be resolved when ICacheService is requested without a key.
    /// Must be one of the keys present in ActiveProviders.
    /// </summary>
    public string? DefaultProvider { get; set; }

    /// <summary>
    /// Defines the behavior when multiple providers are specified for an operation (e.g. in a composite cache).
    /// "Fallback": Try in order, first success wins for reads. Writes go to all.
    /// "Broadcast": Writes go to all. Reads can be configured (e.g., from primary or fastest).
    /// (This is a placeholder for more advanced composite behavior; initial setup will focus on individual providers)
    /// </summary>
    public string? MultiProviderStrategy { get; set; } // Example: "Fallback", "Broadcast"
}

