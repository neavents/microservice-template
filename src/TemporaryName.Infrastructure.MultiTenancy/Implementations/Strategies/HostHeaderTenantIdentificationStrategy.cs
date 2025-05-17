using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Strategies;

public class HostHeaderTenantIdentificationStrategy : ITenantIdentificationStrategy
{
    private readonly ILogger<HostHeaderTenantIdentificationStrategy> _logger;
    // Could add an option to specify if the port should be included or excluded,
    // or a regex to extract the tenant identifier from the host.
    // For now, it's simple: host part only.
    // private readonly TenantResolutionStrategyOptions _strategyOptions;

    public HostHeaderTenantIdentificationStrategy(
        TenantResolutionStrategyOptions strategyOptions,
        ILogger<HostHeaderTenantIdentificationStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(strategyOptions, nameof(strategyOptions));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _logger = logger;
        // _strategyOptions = strategyOptions; // Store if needed for more complex logic

        if (!string.IsNullOrWhiteSpace(strategyOptions.ParameterName))
        {
            _logger.LogInformation("HostHeaderTenantIdentificationStrategy: ParameterName '{ParameterName}' was provided in options. This strategy currently does not use it but could be extended.", strategyOptions.ParameterName);
        }
        _logger.LogInformation("HostHeaderTenantIdentificationStrategy initialized.");
    }

    public int Priority => throw new NotImplementedException();

    public Task<string?> IdentifyTenantAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        if (context.Request == null)
        {
            _logger.LogWarning("HttpContext.Request is null. Cannot identify tenant using HostHeader strategy.");
            return Task.FromResult<string?>(null);
        }

        if (!context.Request.Host.HasValue || string.IsNullOrWhiteSpace(context.Request.Host.Host))
        {
            _logger.LogDebug("Host header is missing, empty, or has no value. Cannot identify tenant using HostHeader strategy.");
            return Task.FromResult<string?>(null);
        }

        string fullHost = context.Request.Host.Host;
        string identifier = fullHost.Split(':')[0];

        if (string.IsNullOrWhiteSpace(identifier))
        {
            _logger.LogDebug("After splitting port, the host identifier part is empty for host '{FullHost}'.", fullHost);
            return Task.FromResult<string?>(null);
        }

        _logger.LogDebug("HostHeaderTenantIdentificationStrategy: Identified potential tenant identifier '{TenantIdentifier}' from host '{FullHost}'.", identifier, fullHost);
        return Task.FromResult<string?>(identifier);
    }
}