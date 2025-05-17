using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Thrown when an operation attempts to violate data residency or region policies for a tenant.
/// </summary>
public class TenantDataRegionViolationException : TenantSecurityException
{
    public string? ConfiguredDataRegion { get; }
    public string? AttemptedDataRegion { get; }

    public TenantDataRegionViolationException(Error error, string tenantId, string? configuredDataRegion, string? attemptedDataRegion)
        : base(error, tenantId)
    {
        ConfiguredDataRegion = configuredDataRegion;
        AttemptedDataRegion = attemptedDataRegion;
    }
    public TenantDataRegionViolationException(Error error, Exception innerException, string tenantId, string? configuredDataRegion, string? attemptedDataRegion)
        : base(error, innerException, tenantId)
    {
        ConfiguredDataRegion = configuredDataRegion;
        AttemptedDataRegion = attemptedDataRegion;
    }
}