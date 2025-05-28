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

public partial class RouteValueTenantIdentificationStrategy : ITenantIdentificationStrategy
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
                LogMissingRouteValueKeyParameter(_logger, error.Code, error.Description);

                throw new InvalidTenantResolutionStrategyParameterException(error, nameof(TenantResolutionStrategyType.RouteValue), nameof(strategyOptions.ParameterName));
            }
            _routeValueKey = strategyOptions.ParameterName;

            LogInitializationSuccess(_logger, _routeValueKey);
        }

    public int Priority => throw new NotImplementedException();

    public Task<string?> IdentifyTenantAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            RouteData? routeData = context.GetRouteData();
            if (routeData is null)
            {
                LogRouteDataNull(_logger);
                return Task.FromResult<string?>(null);
            }

            if (routeData.Values.TryGetValue(_routeValueKey, out object? routeValueObject))
            {
                if (routeValueObject is null)
                {
                    LogRouteValueFoundButIsNull(_logger, _routeValueKey);
                    return Task.FromResult<string?>(null);
                }

                string? tenantIdentifier = routeValueObject.ToString();
                if (!string.IsNullOrWhiteSpace(tenantIdentifier))
                {
                    LogTenantIdentifiedFromRouteValue(_logger, tenantIdentifier, _routeValueKey);
                    return Task.FromResult<string?>(tenantIdentifier);
                }
                LogRouteValueStringNullOrWhitespace(_logger, _routeValueKey, routeValueObject);
            }
            else
            {
                LogRouteValueKeyNotFound(_logger, _routeValueKey);
            }
            return Task.FromResult<string?>(null);
        }
    }