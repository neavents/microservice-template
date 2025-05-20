using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Strategies;

public partial class HostHeaderTenantIdentificationStrategy : ITenantIdentificationStrategy
{
    private readonly ILogger<HostHeaderTenantIdentificationStrategy> _logger;

    public HostHeaderTenantIdentificationStrategy(
        TenantResolutionStrategyOptions strategyOptions,
        ILogger<HostHeaderTenantIdentificationStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(strategyOptions, nameof(strategyOptions));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(strategyOptions.ParameterName))
        {
            LogParameterNameProvidedButUnused(_logger, strategyOptions.ParameterName);
        }

        LogInitializationSuccess(_logger);
    }

    public int Priority => throw new NotImplementedException();

    public Task<string?> IdentifyTenantAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        if (context.Request is null)
        {
            LogHttpContextRequestNull(_logger);
            return Task.FromResult<string?>(null);
        }

        if (!context.Request.Host.HasValue || string.IsNullOrWhiteSpace(context.Request.Host.Host))
        {
            LogHostHeaderMissingOrEmpty(_logger);
            return Task.FromResult<string?>(null);
        }

        string fullHost = context.Request.Host.Host;
        string identifier = fullHost.Split(':')[0];

        if (string.IsNullOrWhiteSpace(identifier))
        {
            LogHostIdentifierEmptyAfterSplit(_logger, fullHost);
            return Task.FromResult<string?>(null);
        }

        LogTenantIdentifiedFromHost(_logger, identifier, fullHost);
        return Task.FromResult<string?>(identifier);
    }
}