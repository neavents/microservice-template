using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TemporaryName.Infrastructure.Security.Authorization.Definitions; // For AuthorizationConstants
using TemporaryName.Infrastructure.Security.Authorization.Requirements;

namespace TemporaryName.Infrastructure.Security.Authorization.Handlers;

public class AnyPermissionAuthorizationHandler : PermissionAuthorizationHandlerBase<HasAnyPermissionRequirement>
{
    public AnyPermissionAuthorizationHandler(ILogger<AnyPermissionAuthorizationHandler> logger)
        : base(logger) { }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasAnyPermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(requirement, nameof(requirement));

        ClaimsPrincipal? user = context.User;
        if (user == null)
        {
            Logger.LogWarning("User principal is null in {HandlerName} for requirement {Requirement}.",
                nameof(AnyPermissionAuthorizationHandler),
                requirement);
            return Task.CompletedTask;
        }

        string userId = GetUserId(user);
        string? foundPermission = null;

        foreach (string requiredPermission in requirement.Permissions)
        {
            if (UserHasPermission(user, requiredPermission))
            {
                foundPermission = requiredPermission;
                break;
            }
        }

        if (foundPermission != null)
        {
            Logger.LogInformation("User '{UserId}' SATISFIED requirement '{Requirement}' due to possessing permission '{FoundPermission}'.",
                userId,
                requirement,
                foundPermission);
            context.Succeed(requirement);
        }
        else
        {
            Logger.LogWarning("User '{UserId}' FAILED requirement '{Requirement}'. User's permissions of type '{ClaimType}': [{UserPermissions}]",
                userId,
                requirement,
                AuthorizationConstants.PermissionClaimType, // Using the constant from this project
                string.Join(", ", GetUserPermissions(user)));
        }
        return Task.CompletedTask;
    }
}
