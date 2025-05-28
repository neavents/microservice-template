using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Thrown specifically when a tenant is deactivated. Inherits from TenantNotActiveException.
/// </summary>
public class TenantDeactivatedException : TenantNotActiveException
{
    public TenantDeactivatedException(string tenantId, Error error) : base(tenantId, "Deactivated", error) { }
    public TenantDeactivatedException(string tenantId, Error error, Exception innerException) : base(tenantId, "Deactivated", error, innerException) { }
}
