using System;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;

namespace TemporaryName.Infrastructure.MultiTenancy.Configuration;

public class TenantConfigurationEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for the tenant.
    /// This should match the key if this entry is part of a dictionary (e.g., in appsettings).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable name of the tenant.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the name or key used to resolve the tenant's database connection string.
    /// This is NOT the raw connection string itself for security.
    /// </summary>
    public string? ConnectionStringName { get; set; }

    /// <summary>
    /// Gets or sets the current operational status of the tenant.
    /// Defaults to Active, assuming configured tenants are intended to be operational.
    /// </summary>
    public TenantStatus Status { get; set; } = TenantStatus.Active;

    /// <summary>
    /// Gets or sets the primary domain associated with the tenant (e.g., "customer1.myapp.com").
    /// Used by host-based tenant resolution strategies.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Gets or sets the identifier for the tenant's subscription tier or plan.
    /// </summary>
    public string? SubscriptionTier { get; set; }

    /// <summary>
    /// Gets or sets the name used for branding purposes within the application for this tenant.
    /// </summary>
    public string? BrandingName { get; set; }

    /// <summary>
    /// Gets or sets the URL (as a string) for the tenant's logo.
    /// Consider validating this as a Uri in your loading logic if needed.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the data isolation mode employed for this tenant.
    /// Defaults to shared database, shared schema.
    /// </summary>
    public TenantDataIsolationMode DataIsolationMode { get; set; } = TenantDataIsolationMode.SharedDatabaseSharedSchema;

    /// <summary>
    /// Gets or sets a list of enabled feature flags or keys for this tenant.
    /// </summary>
    public List<string> EnabledFeatures { get; set; } = new();

    /// <summary>
    /// Gets or sets a dictionary of custom properties or extended attributes for the tenant.
    /// </summary>
    public Dictionary<string, string> CustomProperties { get; set; } = new();

    /// <summary>
    /// Gets or sets the preferred locale for the tenant (e.g., "en-US", "tr-TR").
    /// </summary>
    public string? PreferredLocale { get; set; }

    /// <summary>
    /// Gets or sets the tenant's primary time zone identifier (e.g., "Europe/Istanbul").
    /// </summary>
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// Gets or sets the designated geographical or logical region for data storage.
    /// </summary>
    public string? DataRegion { get; set; }

    /// <summary>
    /// Gets or sets the identifier of a parent tenant, if this tenant is part of a hierarchy.
    /// </summary>
    public string? ParentTenantId { get; set; }

    // Note: Properties like CreatedAtUtc, UpdatedAtUtc, ConcurrencyStamp are typically
    // managed by the persistence layer of the tenant store, not static configuration.
}
