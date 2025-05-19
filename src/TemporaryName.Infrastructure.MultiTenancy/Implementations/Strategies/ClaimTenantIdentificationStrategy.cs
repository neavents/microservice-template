using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;
using TemporaryName.Infrastructure.MultiTenancy.Exceptions;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Strategies;

public partial class ClaimTenantIdentificationStrategy : ITenantIdentificationStrategy
    {
        private readonly string _tenantIdClaimType;
        private readonly ILogger<ClaimTenantIdentificationStrategy> _logger;

        public ClaimTenantIdentificationStrategy(TenantResolutionStrategyOptions strategyOptions, ILogger<ClaimTenantIdentificationStrategy> logger)
        {
            ArgumentNullException.ThrowIfNull(strategyOptions, nameof(strategyOptions));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));
            _logger = logger;

            if (string.IsNullOrWhiteSpace(strategyOptions.ParameterName))
            {
                Error error = new("TenantResolution.Strategy.Claim.MissingParameterName", "ClaimTenantIdentificationStrategy requires ParameterName (the claim type) to be configured in TenantResolutionStrategyOptions.");
                _logger.LogCritical(error.Description);
                throw new InvalidTenantResolutionStrategyParameterException(error, nameof(TenantResolutionStrategyType.Claim), nameof(strategyOptions.ParameterName));
            }
            _tenantIdClaimType = strategyOptions.ParameterName;
            _logger.LogInformation("ClaimTenantIdentificationStrategy initialized. Will look for tenant identifier in claim type: '{ClaimType}'.", _tenantIdClaimType);
        }

    public int Priority => throw new NotImplementedException();

    public Task<string?> IdentifyTenantAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            if (context.User?.Identity == null) 
            {
                 _logger.LogDebug("ClaimTenantIdentificationStrategy: HttpContext.User or User.Identity is null. Cannot identify tenant from claim.");
                 return Task.FromResult<string?>(null);
            }

            if (context.User.Identity.IsAuthenticated != true)
            {
                _logger.LogDebug("ClaimTenantIdentificationStrategy: User is not authenticated. Cannot identify tenant from claim.");
                return Task.FromResult<string?>(null);
            }

            Claim? tenantIdClaim = context.User.FindFirst(_tenantIdClaimType);
            if (tenantIdClaim == null)
            {
                string userIdForLog = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.Identity.Name ?? "unidentified_user";
                _logger.LogDebug("ClaimTenantIdentificationStrategy: Tenant ID claim '{ClaimType}' not found for authenticated user '{UserId}'.", _tenantIdClaimType, userIdForLog);
                return Task.FromResult<string?>(null);
            }

            if (string.IsNullOrWhiteSpace(tenantIdClaim.Value))
            {
                string userIdForLog = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.Identity.Name ?? "unidentified_user";
                _logger.LogDebug("ClaimTenantIdentificationStrategy: Tenant ID claim '{ClaimType}' found for user '{UserId}', but its value is null or whitespace.", _tenantIdClaimType, userIdForLog);
                return Task.FromResult<string?>(null);
            }

            _logger.LogDebug("ClaimTenantIdentificationStrategy: Identified potential tenant identifier '{TenantIdentifier}' from claim '{ClaimType}'.", tenantIdClaim.Value, _tenantIdClaimType);
            return Task.FromResult<string?>(tenantIdClaim.Value);
        }
    }