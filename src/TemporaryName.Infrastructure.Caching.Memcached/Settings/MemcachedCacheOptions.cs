using System;
using System.ComponentModel.DataAnnotations;
using Enyim.Caching.Configuration;

namespace TemporaryName.Infrastructure.Caching.Memcached.Settings;

public class MemcachedCacheOptions
{
    public const string SectionName = "Caching:Memcached";
    [Required]
    public List<string> Servers { get; set; } = []; // ["host1:11211", "host2:11211"]

    public string? InstanceName { get; set; }

    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Zone { get; set; }

    /// <summary>
    /// Default item expiration in seconds if not specified in CacheEntryOptions.
    /// Memcached typically uses seconds for TTL.
    /// </summary>
    public uint DefaultExpirationSeconds { get; set; } = 1800;

    /// <summary>
    /// If set to true, it implies that the configured Memcached server endpoints are expected to be SSL-terminated
    /// (e.g., by a proxy like stunnel or an SSL-enabled Memcached server).
    /// The EnyimMemcachedCore client itself doesn't have explicit SSL/TLS negotiation flags beyond connecting to the given endpoint.
    /// This flag is more for documentation and configuration intent.
    /// </summary>
    public bool UseSslEndpoints { get; set; } = false;

    //public SocketPoolConfigurationOptions PoolConfiguration { get; set; } = new();
}
