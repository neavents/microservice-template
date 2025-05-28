using TemporaryName.Infrastructure.MultiTenancy.Configuration;

namespace TemporaryName.Infrastructure.MultiTenancy.Abstractions;

public record RemoteTenantDto
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? ConnectionStringName { get; set; }
    public TenantStatus Status { get; set; }
    public string? Domain { get; set; }
    public string? SubscriptionTier { get; set; }
    public string? BrandingName { get; set; }
    public string? LogoUrl { get; set; }
    public TenantDataIsolationMode DataIsolationMode { get; set; }
    public List<string>? EnabledFeatures { get; set; }
    public Dictionary<string, string>? CustomProperties { get; set; }
    public string? PreferredLocale { get; set; }
    public string? TimeZoneId { get; set; }
    public string? DataRegion { get; set; }
    public string? ParentTenantId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? ConcurrencyStamp { get; set; }
    public string LookupIdentifier { get; set; } = string.Empty;
}
