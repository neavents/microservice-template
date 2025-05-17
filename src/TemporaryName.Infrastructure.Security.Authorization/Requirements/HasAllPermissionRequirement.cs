using System;
using Microsoft.AspNetCore.Authorization;

namespace TemporaryName.Infrastructure.Security.Authorization.Requirements;

public class HasAllPermissionsRequirement : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; }

    public HasAllPermissionsRequirement(IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        List<string> permissionList = [.. permissions];

        if (permissionList.Count == 0)
        {
            throw new ArgumentException("Permissions collection cannot be empty.", nameof(permissions));
        }
        if (permissionList.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("One or more permissions in the collection are null or whitespace.", nameof(permissions));
        }
        Permissions = permissionList.AsReadOnly();
    }

    public override string ToString() => $"{nameof(HasAllPermissionsRequirement)}: {string.Join(", ", Permissions)}";
}
