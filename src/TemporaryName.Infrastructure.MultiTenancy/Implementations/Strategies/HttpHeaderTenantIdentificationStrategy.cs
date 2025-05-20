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
                LogMissingHeaderNameParameter(_logger, error.Code, error.Description);
                
                throw new InvalidTenantResolutionStrategyParameterException(error, nameof(TenantResolutionStrategyType.HttpHeader), nameof(strategyOptions.ParameterName));
            }
            _headerName = strategyOptions.ParameterName;

            LogInitializationSuccess(_logger, _headerName);
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
            if (context.Request.Headers is null) 
            {
                LogHttpContextRequestHeadersNull(_logger);
                return Task.FromResult<string?>(null);
            }


            if (context.Request.Headers.TryGetValue(_headerName, out StringValues headerValues))
            {
                // A header can technically have multiple values. Usually, for tenant ID, we expect one.
                // Take the first non-empty value.
                string? tenantIdentifier = headerValues.FirstOrDefault(val => !string.IsNullOrWhiteSpace(val));

                if (!string.IsNullOrWhiteSpace(tenantIdentifier))
                {
                    LogTenantIdentifiedFromHeader(_logger, tenantIdentifier, _headerName);
                    return Task.FromResult<string?>(tenantIdentifier);
                }

                if (headerValues.Count != 0)
                {
                    LogHeaderFoundButValueNullOrWhitespace(_logger, _headerName);
                }
                else
                {
                    LogHeaderFoundButEmpty(_logger, _headerName);
                }
            }
            else
            {
                LogHeaderNotFound(_logger, _headerName);
            }
            
            return Task.FromResult<string?>(null);
        }
    }