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

public partial class HttpHeaderTenantIdentificationStrategy : ITenantIdentificationStrategy
    {
        private readonly string _headerName;
        private readonly ILogger<HttpHeaderTenantIdentificationStrategy> _logger;

        public HttpHeaderTenantIdentificationStrategy(TenantResolutionStrategyOptions strategyOptions, ILogger<HttpHeaderTenantIdentificationStrategy> logger)
        {
            ArgumentNullException.ThrowIfNull(strategyOptions, nameof(strategyOptions));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));
            _logger = logger;

            if (string.IsNullOrWhiteSpace(strategyOptions.ParameterName))
            {
                Error error = new("TenantResolution.Strategy.HttpHeader.MissingParameterName", "HttpHeaderTenantIdentificationStrategy requires ParameterName (the HTTP header name) to be configured in TenantResolutionStrategyOptions.");
                _logger.LogCritical(error.Description);
                throw new InvalidTenantResolutionStrategyParameterException(error, nameof(TenantResolutionStrategyType.HttpHeader), nameof(strategyOptions.ParameterName));
            }
            _headerName = strategyOptions.ParameterName;
            _logger.LogInformation("HttpHeaderTenantIdentificationStrategy initialized. Will look for tenant identifier in HTTP header: '{HeaderName}'.", _headerName);
        }

    public int Priority => throw new NotImplementedException();

    public Task<string?> IdentifyTenantAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            if (context.Request == null)
            {
                 _logger.LogWarning("HttpContext.Request is null. Cannot identify tenant using HttpHeader strategy.");
                 return Task.FromResult<string?>(null);
            }
            if (context.Request.Headers == null) 
            {
                _logger.LogWarning("HttpContext.Request.Headers is null. Cannot identify tenant using HttpHeader strategy.");
                return Task.FromResult<string?>(null);
            }


            if (context.Request.Headers.TryGetValue(_headerName, out StringValues headerValues))
            {
                // A header can technically have multiple values. Usually, for tenant ID, we expect one.
                // Take the first non-empty value.
                string? tenantIdentifier = headerValues.FirstOrDefault(val => !string.IsNullOrWhiteSpace(val));

                if (!string.IsNullOrWhiteSpace(tenantIdentifier))
                {
                    _logger.LogDebug("HttpHeaderTenantIdentificationStrategy: Identified potential tenant identifier '{TenantIdentifier}' from HTTP header '{HeaderName}'.", tenantIdentifier, _headerName);
                    return Task.FromResult<string?>(tenantIdentifier);
                }

                if (headerValues.Count != 0) // Header was present but all values were null/whitespace
                {
                    _logger.LogDebug("HttpHeaderTenantIdentificationStrategy: HTTP header '{HeaderName}' found, but its value(s) are null or whitespace.", _headerName);
                }
                else // Header was present but empty (e.g. "X-Tenant-ID: ")
                {
                     _logger.LogDebug("HttpHeaderTenantIdentificationStrategy: HTTP header '{HeaderName}' found, but it is empty.", _headerName);
                }
            }
            else
            {
                _logger.LogDebug("HttpHeaderTenantIdentificationStrategy: HTTP header '{HeaderName}' not found in the request.", _headerName);
            }
            return Task.FromResult<string?>(null);
        }
    }