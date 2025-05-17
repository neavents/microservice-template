using System;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;

namespace TemporaryName.Infrastructure.MultiTenancy.Settings;

public class TenantResolutionStrategyOptions
{
    /// <summary>
    /// Gets or sets the type of resolution strategy.
    /// </summary>
    public TenantResolutionStrategyType Type { get; set; }

    /// <summary>
    /// Gets or sets the parameter name relevant to the strategy type.
    /// Examples:
    /// - For <see cref="TenantResolutionStrategyType.HttpHeader"/>: The name of the HTTP header (e.g., "X-Tenant-ID").
    /// - For <see cref="TenantResolutionStrategyType.QueryString"/>: The name of the query parameter (e.g., "tenantId").
    /// - For <see cref="TenantResolutionStrategyType.RouteValue"/>: The name of the route parameter (e.g., "tenant").
    /// - For <see cref="TenantResolutionStrategyType.Claim"/>: The type of the claim (e.g., "http://schemas.microsoft.com/identity/claims/tenantid").
    /// Not used for <see cref="TenantResolutionStrategyType.HostHeader"/>.
    /// </summary>
    public string? ParameterName { get; set; }

    /// <summary>
    /// Gets or sets the order in which this strategy should be attempted.
    /// Strategies with lower order values are tried first.
    /// Defaults to 0.
    /// </summary>
    public int Order { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether this strategy is enabled.
    /// Defaults to true. Allows defining strategies in configuration but easily toggling them.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
