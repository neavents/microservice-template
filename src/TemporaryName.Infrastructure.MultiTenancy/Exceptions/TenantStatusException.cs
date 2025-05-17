using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Base class for exceptions related to a tenant's operational status.
/// </summary>
public abstract class TenantStatusException : MultiTenancyException
{
    public string TenantId { get; }
    public string? CurrentStatus { get; } // Could be TenantStatus.ToString()

    protected TenantStatusException(string tenantId, string? currentStatus, Error error) : base(error)
    {
        TenantId = tenantId;
        CurrentStatus = currentStatus;
    }
    protected TenantStatusException(string tenantId, string? currentStatus, Error error, Exception innerException) : base(error, innerException)
    {
        TenantId = tenantId;
        CurrentStatus = currentStatus;
    }
}
