using System;
using Microsoft.AspNetCore.Authorization;

namespace TemporaryName.Infrastructure.Security.Authorization.Requirements;

public class HasPermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public HasPermissionRequirement(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            // Consider using a shared/custom ArgumentNullException or ArgumentException subclass
            throw new ArgumentException("Permission cannot be null or whitespace.", nameof(permission));
        }
        Permission = permission;
    }

    public override string ToString() => $"{nameof(HasPermissionRequirement)}: {Permission}";
}
