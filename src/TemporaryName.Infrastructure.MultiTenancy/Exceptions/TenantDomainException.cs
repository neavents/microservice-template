using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Represents an error that occurs due to a violation of domain rules or invariants within the tenant entity itself, during its creation or modification.
/// This is distinct from configuration or resolution issues.
/// </summary>
public class TenantDomainException : MultiTenancyException 
{
    public string? TenantIdAttempted { get; }

    public TenantDomainException(Error error, string? tenantIdAttempted = null)
        : base(error)
    {
        TenantIdAttempted = tenantIdAttempted;
    }

    public TenantDomainException(Error error, Exception innerException, string? tenantIdAttempted = null)
        : base(error, innerException)
    {
        TenantIdAttempted = tenantIdAttempted;
    }

    public TenantDomainException(string message, Error error, string? tenantIdAttempted = null)
        : base(message, error)
    {
        TenantIdAttempted = tenantIdAttempted;
    }

    public TenantDomainException(string message, Error error, Exception innerException, string? tenantIdAttempted = null)
        : base(message, error, innerException)
    {
        TenantIdAttempted = tenantIdAttempted;
    }
}
