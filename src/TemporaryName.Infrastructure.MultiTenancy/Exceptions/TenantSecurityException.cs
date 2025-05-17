using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Represents errors related to tenant security, authorization, or data compliance.
/// </summary>
public class TenantSecurityException : MultiTenancyException
{
    public string? TenantId { get; }

    public TenantSecurityException(Error error, string? tenantId = null) : base(error)
    {
        TenantId = tenantId;
    }
    public TenantSecurityException(Error error, Exception innerException, string? tenantId = null) : base(error, innerException)
    {
        TenantId = tenantId;
    }
}
