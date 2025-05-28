using System;

namespace TemporaryName.Infrastructure.MultiTenancy.Settings;

/// <summary>
/// Configures how requests that do not resolve to a specific tenant (host requests) are handled.
/// </summary>
public class HostHandlingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether requests that do not resolve to a specific tenant
    /// are allowed to proceed with a null tenant context.
    /// If false (default), and no <see cref="MapToTenantIdentifier"/> is set,
    /// such requests might be rejected based on <see cref="MultiTenancyOptions.ThrowIfTenantMissing"/>.
    /// </summary>
    public bool AllowUnresolvedRequests { get; set; } = false;

    /// <summary>
    /// Optional. If set, unresolved host requests (where no tenant is identified by strategies)
    /// will be mapped to this specific tenant identifier. This allows a "default" or "shared services"
    /// tenant experience for non-tenant-specific parts of the application (e.g., main landing page).
    /// </summary>
    public string? MapToTenantIdentifier { get; set; }
}
