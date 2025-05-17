using System;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;

namespace TemporaryName.Infrastructure.MultiTenancy.Abstractions;

public interface ITenantInfo
{
    /// <summary>
    /// Gets the unique identifier for the tenant. (e.g., "acme-corp", "user-guid")
    /// Consider Guid for non-human-readable, globally unique IDs.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the human-readable name of the tenant. (e.g., "Acme Corporation")
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the name or key used to resolve the tenant's database connection string.
    /// This is NOT the raw connection string.
    /// (e.g., "AcmeCorpConnection", "Tenant123_DB_Config")
    /// </summary>
    string? ConnectionStringName { get; }

    /// <summary>
    /// Gets the current operational status of the tenant.
    /// </summary>
    TenantStatus Status { get; }

    /// <summary>
    /// Gets the primary domain associated with the tenant. (e.g., "acme.temporaryname.com")
    /// </summary>
    string? Domain { get; }

    /// <summary>
    /// Gets the identifier for the tenant's subscription tier or plan. (e.g., "premium_plus", "enterprise_annual")
    /// </summary>
    string? SubscriptionTier { get; }

    /// <summary>
    /// Gets the name used for branding purposes. (e.g., "Acme Solutions by TemporaryName")
    /// </summary>
    string? BrandingName { get; }

    /// <summary>
    /// Gets the URL for the tenant's logo.
    /// </summary>
    Uri? LogoUrl { get; }

    /// <summary>
    /// Gets the Coordinated Universal Time (UTC) when the tenant was created.
    /// </summary>
    DateTimeOffset CreatedAtUtc { get; }

    /// <summary>
    /// Gets the Coordinated Universal Time (UTC) when the tenant's information was last updated.
    /// Nullable if never updated.
    /// </summary>
    DateTimeOffset? UpdatedAtUtc { get; }

    /// <summary>
    /// Gets a concurrency stamp (e.g., ETag, row version) for optimistic concurrency control.
    /// </summary>
    string? ConcurrencyStamp { get; }

    /// <summary>
    /// Gets the data isolation mode employed for this tenant.
    /// </summary>
    TenantDataIsolationMode DataIsolationMode { get; }

    /// <summary>
    /// Gets a set of enabled feature flags or keys for this tenant.
    /// (e.g., "beta_reporting_access", "advanced_analytics_ui")
    /// </summary>
    IReadOnlySet<string> EnabledFeatures { get; }

    /// <summary>
    /// Gets a dictionary of custom properties or extended attributes for the tenant.
    /// (e.g., {"internalProjectCode": "ProjectPhoenix", "migrationWave": "3"})
    /// </summary>
    IReadOnlyDictionary<string, string> CustomProperties { get; }

    /// <summary>
    /// Gets the preferred locale for the tenant, influencing language and formatting.
    /// Stored as an IETF language tag (e.g., "en-US", "de-DE", "tr-TR").
    /// Nullable if the system default should be used or if not applicable.
    /// </summary>
    string? PreferredLocale { get; }

    /// <summary>
    /// Gets the tenant's primary time zone identifier.
    /// Stored using IANA Time Zone Database names (e.g., "Europe/Istanbul", "America/New_York").
    /// Nullable if UTC is always assumed or if not critical for tenant operations.
    /// </summary>
    string? TimeZoneId { get; }

    /// <summary>
    /// Gets the designated geographical or logical region for the tenant's primary data storage and processing.
    /// This is crucial for data sovereignty and compliance (e.g., "EU-West-1", "US-East-2", "Asia-Pacific-Tokyo").
    /// This identifier can be used by infrastructure layers to route requests or provision resources appropriately.
    /// Nullable if region is not a primary concern or is handled implicitly.
    /// </summary>
    string? DataRegion { get; }

    /// <summary>
    /// Gets the identifier of a parent tenant, if this tenant is part of a hierarchy.
    /// (e.g., a regional office tenant under a global HQ tenant).
    /// Nullable if the tenant is top-level or hierarchies are not used.
    /// This allows for modeling organizational structures and potentially rolling up billing or reporting.
    /// </summary>
    string? ParentTenantId { get; }
}
