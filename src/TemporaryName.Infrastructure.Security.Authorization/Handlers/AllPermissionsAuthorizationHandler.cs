using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TemporaryName.Infrastructure.Security.Authorization.Definitions; // For AuthorizationConstants
using TemporaryName.Infrastructure.Security.Authorization.Requirements;

namespace TemporaryName.Infrastructure.Security.Authorization.Handlers;

public class AllPermissionsAuthorizationHandler : PermissionAuthorizationHandlerBase<HasAllPermissionsRequirement>
{
    public AllPermissionsAuthorizationHandler(ILogger<AllPermissionsAuthorizationHandler> logger)
        : base(logger) { }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasAllPermissionsRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(requirement, nameof(requirement));

        ClaimsPrincipal? user = context.User;
        if (user == null)
        {
            Logger.LogWarning("User principal is null in {HandlerName} for requirement {Requirement}.",
                nameof(AllPermissionsAuthorizationHandler),
                requirement);
            return Task.CompletedTask;
        }

        string userId = GetUserId(user);
        HashSet<string> userPermissionValues = GetUserPermissions(user)
            .ToHashSet(StringComparer.Ordinal);

        bool allPermissionsFound = true;
        List<string> missingPermissions = new();

        foreach (string requiredPermission in requirement.Permissions)
        {
            if (!userPermissionValues.Contains(requiredPermission))
            {
                allPermissionsFound = false;
                missingPermissions.Add(requiredPermission);
            }
        }

        if (allPermissionsFound)
        {
            Logger.LogInformation("User '{UserId}' SATISFIED requirement '{Requirement}'.", userId, requirement);
            context.Succeed(requirement);
        }
        else
        {
            Logger.LogWarning("User '{UserId}' FAILED requirement '{Requirement}'. Missing permissions: [{MissingPermissions}]. User's permissions of type '{ClaimType}': [{UserPermissions}]",
                userId,
                requirement,
                string.Join(", ", missingPermissions),
                AuthorizationConstants.PermissionClaimType, // Using the constant from this project
                string.Join(", ", userPermissionValues));
        }
        return Task.CompletedTask;
    }
}