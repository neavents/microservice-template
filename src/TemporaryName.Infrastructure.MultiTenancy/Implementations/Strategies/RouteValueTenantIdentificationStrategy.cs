using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;
using TemporaryName.Infrastructure.MultiTenancy.Exceptions;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Strategies;

public class RouteValueTenantIdentificationStrategy : ITenantIdentificationStrategy
    {
        private readonly string _routeValueKey;
        private readonly ILogger<RouteValueTenantIdentificationStrategy> _logger;

        public RouteValueTenantIdentificationStrategy(TenantResolutionStrategyOptions strategyOptions, ILogger<RouteValueTenantIdentificationStrategy> logger)
        {
            ArgumentNullException.ThrowIfNull(strategyOptions, nameof(strategyOptions));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));
            _logger = logger;

            if (string.IsNullOrWhiteSpace(strategyOptions.ParameterName))
            {
                Error error = new("TenantResolution.Strategy.RouteValue.MissingParameterName", "RouteValueTenantIdentificationStrategy requires ParameterName (the route value key) to be configured in TenantResolutionStrategyOptions.");
                _logger.LogCritical(error.Description);
                throw new InvalidTenantResolutionStrategyParameterException(error, nameof(TenantResolutionStrategyType.RouteValue), nameof(strategyOptions.ParameterName));
            }
            _routeValueKey = strategyOptions.ParameterName;
            _logger.LogInformation("RouteValueTenantIdentificationStrategy initialized. Will look for tenant identifier in route value key: '{RouteValueKey}'.", _routeValueKey);
        }

    public int Priority => throw new NotImplementedException();

    public Task<string?> IdentifyTenantAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            RouteData? routeData = context.GetRouteData();
            if (routeData == null)
            {
                _logger.LogDebug("RouteValueTenantIdentificationStrategy: RouteData is null for the current request. This strategy requires routing to be active.");
                return Task.FromResult<string?>(null);
            }

            if (routeData.Values.TryGetValue(_routeValueKey, out object? routeValueObject))
            {
                if (routeValueObject == null)
                {
                    _logger.LogDebug("RouteValueTenantIdentificationStrategy: Route value key '{RouteValueKey}' found, but its value is null.", _routeValueKey);
                    return Task.FromResult<string?>(null);
                }

                string? tenantIdentifier = routeValueObject.ToString();
                if (!string.IsNullOrWhiteSpace(tenantIdentifier))
                {
                    _logger.LogDebug("RouteValueTenantIdentificationStrategy: Identified potential tenant identifier '{TenantIdentifier}' from route value key '{RouteValueKey}'.", tenantIdentifier, _routeValueKey);
                    return Task.FromResult<string?>(tenantIdentifier);
                }
                _logger.LogDebug("RouteValueTenantIdentificationStrategy: Route value key '{RouteValueKey}' found, but its string representation is null or whitespace. Original value: '{OriginalValue}'", _routeValueKey, routeValueObject);
            }
            else
            {
                _logger.LogDebug("RouteValueTenantIdentificationStrategy: Route value key '{RouteValueKey}' not found in RouteData.Values.", _routeValueKey);
            }
            return Task.FromResult<string?>(null);
        }
    }