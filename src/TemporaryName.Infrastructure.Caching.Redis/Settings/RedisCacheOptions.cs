using System;
using System.ComponentModel.DataAnnotations;

namespace TemporaryName.Infrastructure.Caching.Redis.Settings;

public class RedisCacheOptions
{
    public const string SectionName = "Caching:Redis";

    /// <summary>
    /// The Redis connection string.
    /// e.g., "localhost:6379,password=yourpassword"
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Optional instance name to prefix all cache keys.
    /// Useful if multiple applications share the same Redis instance.
    /// </summary>
    public string? InstanceName { get; set; }

    /// <summary>
    /// Indicates whether SSL/TLS should be used for the connection.
    /// This can also often be controlled via the ConnectionString (e.g., "ssl=true").
    /// </summary>
    public bool Ssl { get; set; } = false;

    /// <summary>
    /// The password for Redis authentication.
    /// For Redis 6+, consider using ACLs where the username can be part of the password field
    /// or managed through dedicated ACL configurations on the server and client.
    /// This can also be part of the ConnectionString.
    /// </summary>
    public string? Password { get; set; }


    /// <summary>
    /// Default sliding expiration for cache entries if not otherwise specified.
    /// Format: "0.00:30:00" (30 minutes)
    /// </summary>
    public TimeSpan? DefaultSlidingExpiration { get; set; }

    /// <summary>
    /// Default absolute expiration relative to now for cache entries if not otherwise specified.
    /// Format: "1.00:00:00" (1 hour)
    /// </summary>
    public TimeSpan? DefaultAbsoluteExpirationRelativeToNow { get; set; }

    /// <summary>
    /// Specify the SSL host if it differs from the connection host
    /// </summary>
    public string? SslHost { get; set; }

    /// <summary>
    /// For certain commands like SCAN (USE WITH CAUTION - development only)
    /// </summary>
    public bool AllowAdmin { get; set; }

    /// <summary>
    /// Allow untrusted SSL certificates (USE WITH CAUTION - development only)
    /// </summary>
    public bool AllowUntrustedCertificate { get; set; } = false;
}
