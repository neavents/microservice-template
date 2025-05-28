namespace TemporaryName.Infrastructure.MultiTenancy.Configuration;

public enum TenantResolutionStrategyType
{
    /// <summary>
    /// Resolves the tenant based on the request's host header (e.g., subdomain.example.com).
    /// </summary>
    HostHeader = 0,

    /// <summary>
    /// Resolves the tenant based on a custom HTTP header in the request.
    /// </summary>
    HttpHeader = 1,

    /// <summary>
    /// Resolves the tenant based on a query string parameter in the request URL.
    /// </summary>
    QueryString = 2,

    /// <summary>
    /// Resolves the tenant based on a route value in the request path.
    /// </summary>
    RouteValue = 3,

    /// <summary>
    /// Resolves the tenant based on a claim in the authenticated user's identity.
    /// </summary>
    Claim = 4
}
