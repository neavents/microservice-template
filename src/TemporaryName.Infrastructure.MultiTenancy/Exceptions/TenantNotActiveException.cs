using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Thrown when an operation requires an active tenant, but the resolved tenant is not in an active state.
/// </summary>
public class TenantNotActiveException : TenantStatusException
{
    public TenantNotActiveException(string tenantId, string currentStatus, Error error) : base(tenantId, currentStatus, error) { }
    public TenantNotActiveException(string tenantId, string currentStatus, Error error, Exception innerException) : base(tenantId, currentStatus, error, innerException) { }
}
