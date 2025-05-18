using System;
using Microsoft.Extensions.Logging;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;
using TemporaryName.Infrastructure.MultiTenancy.Exceptions;
using TemporaryName.Infrastructure.MultiTenancy.Implementations.Strategies;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Providers;

/// <summary>
/// Provides instances of <see cref="ITenantIdentificationStrategy"/> based on configuration.
/// This class acts as a factory for tenant identification strategies.
/// </summary>
public class TenantStrategyProvider : ITenantStrategyProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantStrategyProvider> _logger;

    public TenantStrategyProvider(IServiceProvider serviceProvider, ILogger<TenantStrategyProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public ITenantIdentificationStrategy GetStrategy(TenantResolutionStrategyOptions strategyOptions)
    {
        ArgumentNullException.ThrowIfNull(strategyOptions, nameof(strategyOptions));

        _logger.LogDebug("Attempting to get strategy for type: {StrategyType}, ParameterName: '{ParameterName}', Order: {Order}",
            strategyOptions.Type, strategyOptions.ParameterName, strategyOptions.Order);

        try
        {
            switch (strategyOptions.Type)
            {
                case TenantResolutionStrategyType.HostHeader:
                    return new HostHeaderTenantIdentificationStrategy(
                        strategyOptions,
                        (ILogger<HostHeaderTenantIdentificationStrategy>)_serviceProvider.GetService(typeof(ILogger<HostHeaderTenantIdentificationStrategy>))!
                    );
                case TenantResolutionStrategyType.HttpHeader:
                    return new HttpHeaderTenantIdentificationStrategy(
                        strategyOptions,
                        (ILogger<HttpHeaderTenantIdentificationStrategy>)_serviceProvider.GetService(typeof(ILogger<HttpHeaderTenantIdentificationStrategy>))!
                    );
                case TenantResolutionStrategyType.QueryString:
                    return new QueryStringTenantIdentificationStrategy(
                        strategyOptions,
                        (ILogger<QueryStringTenantIdentificationStrategy>)_serviceProvider.GetService(typeof(ILogger<QueryStringTenantIdentificationStrategy>))!
                    );
                case TenantResolutionStrategyType.RouteValue:
                    return new RouteValueTenantIdentificationStrategy(
                        strategyOptions,
                        (ILogger<RouteValueTenantIdentificationStrategy>)_serviceProvider.GetService(typeof(ILogger<RouteValueTenantIdentificationStrategy>))!
                    );
                case TenantResolutionStrategyType.Claim:
                    return new ClaimTenantIdentificationStrategy(
                        strategyOptions,
                        (ILogger<ClaimTenantIdentificationStrategy>)_serviceProvider.GetService(typeof(ILogger<ClaimTenantIdentificationStrategy>))!
                    );
                default:
                    Error error = new("TenantResolution.Strategy.UnknownType", $"Unsupported tenant resolution strategy type: {strategyOptions.Type}.");
                    _logger.LogError(error.Description);
                    throw new TenantConfigurationException(error.Description, error);
            }
        }
        catch (InvalidTenantResolutionStrategyParameterException ex)
        {
            _logger.LogError(ex, "Failed to create tenant strategy due to invalid parameters for type {StrategyType}.", strategyOptions.Type);
            throw;
        }
        catch (Exception ex)
        {
            Error error = new("TenantResolution.Strategy.InstantiationFailed", $"Failed to instantiate tenant resolution strategy of type {strategyOptions.Type}. See inner exception for details.");
            _logger.LogCritical(ex, error.Description);
            throw new TenantConfigurationException(error.Description, error, ex);
        }
    }
}
