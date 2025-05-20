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

                LogMissingClaimTypeParameter(_logger, error.Code, error.Description);
                
                throw new InvalidTenantResolutionStrategyParameterException(error, nameof(TenantResolutionStrategyType.Claim), nameof(strategyOptions.ParameterName));
            }
            _tenantIdClaimType = strategyOptions.ParameterName;
            LogInitializationSuccess(_logger, _tenantIdClaimType);
        }

    public int Priority => throw new NotImplementedException();

    public Task<string?> IdentifyTenantAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            if (context.User?.Identity is null) 
            {
                LogUserOrIdentityNull(_logger);
                 return Task.FromResult<string?>(null);
            }

            if (context.User.Identity.IsAuthenticated != true)
            {
                LogUserNotAuthenticated(_logger);
                return Task.FromResult<string?>(null);
            }

            Claim? tenantIdClaim = context.User.FindFirst(_tenantIdClaimType);
            if (tenantIdClaim == null)
            {
                string userIdForLog = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.Identity.Name ?? "unidentified_user";
                
                LogTenantIdClaimNotFound(_logger, _tenantIdClaimType, userIdForLog);
                return Task.FromResult<string?>(null);
            }

            if (string.IsNullOrWhiteSpace(tenantIdClaim.Value))
            {
                string userIdForLog = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.Identity.Name ?? "unidentified_user";

                LogTenantIdClaimValueNullOrWhitespace(_logger, _tenantIdClaimType, userIdForLog);
                return Task.FromResult<string?>(null);
            }
            
            LogTenantIdentifiedFromClaim(_logger, tenantIdClaim.Value, _tenantIdClaimType);
            return Task.FromResult<string?>(tenantIdClaim.Value);
        }
    }