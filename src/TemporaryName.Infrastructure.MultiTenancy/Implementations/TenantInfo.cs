using System;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;
using TemporaryName.Infrastructure.MultiTenancy.Exceptions;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations;

public class TenantInfo : ITenantInfo
{
    public string Id { get; }
    public string? Name { get; }
    public string? ConnectionStringName { get; }
    public TenantStatus Status { get; }
    public string? Domain { get; }
    public string? SubscriptionTier { get; }
    public string? BrandingName { get; }
    public Uri? LogoUrl { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? UpdatedAtUtc { get; }
    public string? ConcurrencyStamp { get; }
    public TenantDataIsolationMode DataIsolationMode { get; }
    public IReadOnlySet<string> EnabledFeatures { get; }
    public IReadOnlyDictionary<string, string> CustomProperties { get; }
    public string? PreferredLocale { get; }
    public string? TimeZoneId { get; }
    public string? DataRegion { get; }
    public string? ParentTenantId { get; }

    public TenantInfo(
        string id,
        string? name,
        string? connectionStringName,
        TenantStatus status,
        string? domain,
        string? subscriptionTier,
        string? brandingName,
        Uri? logoUrl,
        TenantDataIsolationMode dataIsolationMode,
        IEnumerable<string>? enabledFeatures, // Changed to IEnumerable for flexibility from config
        IDictionary<string, string>? customProperties, // Changed to IDictionary
        string? preferredLocale,
        string? timeZoneId,
        string? dataRegion,
        string? parentTenantId,
        DateTimeOffset createdAtUtc = default,
        DateTimeOffset? updatedAtUtc = null,
        string? concurrencyStamp = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            // This is a critical failure if we're trying to instantiate a TenantInfo.
            Error error = new("Tenant.Initialization.MissingId", "Tenant ID cannot be null or whitespace during TenantInfo creation.");
 
            throw new TenantDomainException(error.Description!, error, id);
        }

        Id = id;
        Name = name;
        ConnectionStringName = connectionStringName;
        Status = status;
        Domain = domain;
        SubscriptionTier = subscriptionTier;
        BrandingName = brandingName;
        LogoUrl = logoUrl;
        DataIsolationMode = dataIsolationMode; 

        EnabledFeatures = enabledFeatures?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CustomProperties = customProperties?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase)
                           ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        PreferredLocale = preferredLocale;
        TimeZoneId = timeZoneId;
        DataRegion = dataRegion;
        ParentTenantId = parentTenantId;

        CreatedAtUtc = (createdAtUtc == default) ? DateTimeOffset.UtcNow : createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        ConcurrencyStamp = concurrencyStamp;
    }
}
