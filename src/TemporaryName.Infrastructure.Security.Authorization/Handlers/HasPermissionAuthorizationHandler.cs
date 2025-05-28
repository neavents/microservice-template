using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TemporaryName.Infrastructure.Security.Authorization.Definitions;
using TemporaryName.Infrastructure.Security.Authorization.Requirements;

namespace TemporaryName.Infrastructure.Security.Authorization.Handlers;

public class HasPermissionAuthorizationHandler : PermissionAuthorizationHandlerBase<HasPermissionRequirement>
{
    public HasPermissionAuthorizationHandler(ILogger<HasPermissionAuthorizationHandler> logger)
        : base(logger) { }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasPermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(requirement, nameof(requirement));

        ClaimsPrincipal? user = context.User;
        if (user == null)
        {
            Logger.LogWarning("User principal is null in {HandlerName} for requirement {Requirement}.",
                nameof(HasPermissionAuthorizationHandler),
                requirement);
            return Task.CompletedTask;
        }

        string userId = GetUserId(user);

        if (UserHasPermission(user, requirement.Permission))
        {
            Logger.LogInformation("User '{UserId}' SATISFIED requirement '{Requirement}'.", userId, requirement);
            context.Succeed(requirement);
        }
        else
        {
            Logger.LogWarning("User '{UserId}' FAILED requirement '{Requirement}'. Looking for permission '{PermissionValue}'. User's permissions of type '{ClaimType}': [{UserPermissions}]",
                userId,
                requirement,
                requirement.Permission,
                AuthorizationConstants.PermissionClaimType, // Using the constant from this project
                string.Join(", ", GetUserPermissions(user)));
        }
        return Task.CompletedTask;
    }
}