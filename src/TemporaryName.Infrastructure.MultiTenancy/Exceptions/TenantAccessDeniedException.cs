using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Thrown when access to a tenant's resources or a specific operation for a tenant is denied.
/// </summary>
public class TenantAccessDeniedException : TenantSecurityException
{
    public string? UserId { get; }
    public string? RequiredPermission { get; }

    public TenantAccessDeniedException(Error error, string? tenantId = null, string? userId = null, string? requiredPermission = null)
        : base(error, tenantId)
    {
        UserId = userId;
        RequiredPermission = requiredPermission;
    }
    public TenantAccessDeniedException(Error error, Exception innerException, string? tenantId = null, string? userId = null, string? requiredPermission = null)
        : base(error, innerException, tenantId)
    {
        UserId = userId;
        RequiredPermission = requiredPermission;
    }
}