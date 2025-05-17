using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Thrown specifically when a tenant is suspended. Inherits from TenantNotActiveException.
/// </summary>
public class TenantSuspendedException : TenantNotActiveException
{
    public DateTimeOffset? SuspensionEndDate { get; }

    public TenantSuspendedException(string tenantId, Error error, DateTimeOffset? suspensionEndDate = null)
        : base(tenantId, "Suspended", error)
    {
        SuspensionEndDate = suspensionEndDate;
    }
    public TenantSuspendedException(string tenantId, Error error, Exception innerException, DateTimeOffset? suspensionEndDate = null)
        : base(tenantId, "Suspended", error, innerException)
    {
        SuspensionEndDate = suspensionEndDate;
    }
}
