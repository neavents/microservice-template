using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;
using TemporaryName.Infrastructure.Security.Authorization.Definitions;

namespace TemporaryName.Infrastructure.Security.Authorization.Handlers;

public abstract class PermissionAuthorizationHandlerBase<TRequirement>(ILogger logger)
    : AuthorizationHandler<TRequirement> where TRequirement : IAuthorizationRequirement
{
    protected readonly ILogger Logger = logger;

    protected string GetUserId(ClaimsPrincipal user){
        //ArgumentNullException.ThrowIfNull(user);

        return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "UnknownUser";
    }

    protected bool UserHasPermission(ClaimsPrincipal user, string permission)
    {
        if (user == null || string.IsNullOrWhiteSpace(permission)) return false;

        return user.HasClaim(claim =>
            claim.Type == AuthorizationConstants.PermissionClaimType && // Crucially uses the definition from Application.Contracts
            claim.Value.Equals(permission, StringComparison.OrdinalIgnoreCase) // Consider case sensitivity needs
            // Optional: && claim.Issuer == "your-expected-issuer"
        );
    }

    protected IEnumerable<string> GetUserPermissions(ClaimsPrincipal user)
    {
        return user?.FindAll(AuthorizationConstants.PermissionClaimType).Select(c => c.Value) ?? Enumerable.Empty<string>();
    }
}