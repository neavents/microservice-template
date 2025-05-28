using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Thrown when an operation requires a fully provisioned tenant, but the tenant is still provisioning.
/// </summary>
public class TenantProvisioningIncompleteException : TenantStatusException
{
    public TenantProvisioningIncompleteException(string tenantId, Error error) : base(tenantId, "Provisioning", error) { }
    public TenantProvisioningIncompleteException(string tenantId, Error error, Exception innerException) : base(tenantId, "Provisioning", error, innerException) { }
}
