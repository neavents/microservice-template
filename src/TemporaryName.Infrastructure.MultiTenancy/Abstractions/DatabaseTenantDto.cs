namespace TemporaryName.Infrastructure.MultiTenancy.Abstractions;

public record DatabaseTenantDto
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? ConnectionStringName { get; set; } // This is the tenant-specific CS, not the metadata one
    public int Status { get; set; }
    public string? Domain { get; set; }
    public string? SubscriptionTier { get; set; }
    public string? BrandingName { get; set; }
    public string? LogoUrl { get; set; }
    public int DataIsolationMode { get; set; }
    public string? EnabledFeaturesJson { get; set; }
    public string? CustomPropertiesJson { get; set; }
    public string? PreferredLocale { get; set; }
    public string? TimeZoneId { get; set; }
    public string? DataRegion { get; set; }
    public string? ParentTenantId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? ConcurrencyStamp { get; set; }
    public string LookupIdentifier { get; set; } = string.Empty;
}
