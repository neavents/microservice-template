using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;
using TemporaryName.Infrastructure.MultiTenancy.Exceptions;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Strategies;

public partial class QueryStringTenantIdentificationStrategy : ITenantIdentificationStrategy
    {
        private readonly string _queryParameterName;
        private readonly ILogger<QueryStringTenantIdentificationStrategy> _logger;

        public QueryStringTenantIdentificationStrategy(TenantResolutionStrategyOptions strategyOptions, ILogger<QueryStringTenantIdentificationStrategy> logger)
        {
            ArgumentNullException.ThrowIfNull(strategyOptions, nameof(strategyOptions));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));
            _logger = logger;

            if (string.IsNullOrWhiteSpace(strategyOptions.ParameterName))
            {
                Error error = new("TenantResolution.Strategy.QueryString.MissingParameterName", "QueryStringTenantIdentificationStrategy requires ParameterName (the query string key) to be configured in TenantResolutionStrategyOptions.");
                _logger.LogCritical(error.Description);
                throw new InvalidTenantResolutionStrategyParameterException(error, nameof(TenantResolutionStrategyType.QueryString), nameof(strategyOptions.ParameterName));
            }
            _queryParameterName = strategyOptions.ParameterName;
            _logger.LogInformation("QueryStringTenantIdentificationStrategy initialized. Will look for tenant identifier in query parameter: '{QueryParameterName}'.", _queryParameterName);
        }

    public int Priority => throw new NotImplementedException();

    public Task<string?> IdentifyTenantAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            if (context.Request == null)
            {
                 _logger.LogWarning("HttpContext.Request is null. Cannot identify tenant using QueryString strategy.");
                 return Task.FromResult<string?>(null);
            }

            if (context.Request.Query.TryGetValue(_queryParameterName, out StringValues queryValues))
            {
                string? tenantIdentifier = queryValues.FirstOrDefault(val => !string.IsNullOrWhiteSpace(val));

                if (!string.IsNullOrWhiteSpace(tenantIdentifier))
                {
                    _logger.LogDebug("QueryStringTenantIdentificationStrategy: Identified potential tenant identifier '{TenantIdentifier}' from query string parameter '{QueryParameterName}'.", tenantIdentifier, _queryParameterName);
                    return Task.FromResult<string?>(tenantIdentifier);
                }
                if (queryValues.Count != 0)
                {
                    _logger.LogDebug("QueryStringTenantIdentificationStrategy: Query string parameter '{QueryParameterName}' found, but its value(s) are null or whitespace.", _queryParameterName);
                }
                else
                {
                    _logger.LogDebug("QueryStringTenantIdentificationStrategy: Query string parameter '{QueryParameterName}' found, but it is empty.", _queryParameterName);
                }
            }
            else
            {
                _logger.LogDebug("QueryStringTenantIdentificationStrategy: Query string parameter '{QueryParameterName}' not found in the request.", _queryParameterName);
            }
            return Task.FromResult<string?>(null);
        }
    }