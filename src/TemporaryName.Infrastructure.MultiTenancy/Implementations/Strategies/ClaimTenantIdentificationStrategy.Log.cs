using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Strategies;

public partial class ClaimTenantIdentificationStrategy
{
    private const int ClassId = 55;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int EvtMissingClaimTypeParameter = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtInitializationSuccess = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtUserOrIdentityNull = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int EvtUserNotAuthenticated = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int EvtTenantIdClaimNotFound = BaseEventId + (4 * Logging.IncrementPerLog);
    public const int EvtTenantIdClaimValueNullOrWhitespace = BaseEventId + (5 * Logging.IncrementPerLog);
    public const int EvtTenantIdentifiedFromClaim = BaseEventId + (6 * Logging.IncrementPerLog);

    [LoggerMessage(
        EventId = EvtMissingClaimTypeParameter,
        Level = LogLevel.Critical,
        Message = "ClaimTenantIdentificationStrategy requires ParameterName (the claim type) to be configured. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogMissingClaimTypeParameter(ILogger logger, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtInitializationSuccess,
        Level = LogLevel.Information,
        Message = "ClaimTenantIdentificationStrategy initialized. Will look for tenant identifier in claim type: '{ClaimType}'.")]
    public static partial void LogInitializationSuccess(ILogger logger, string claimType);

    [LoggerMessage(
        EventId = EvtUserOrIdentityNull,
        Level = LogLevel.Debug,
        Message = "ClaimTenantIdentificationStrategy: HttpContext.User or User.Identity is null. Cannot identify tenant from claim.")]
    public static partial void LogUserOrIdentityNull(ILogger logger);

    [LoggerMessage(
        EventId = EvtUserNotAuthenticated,
        Level = LogLevel.Debug,
        Message = "ClaimTenantIdentificationStrategy: User is not authenticated. Cannot identify tenant from claim.")]
    public static partial void LogUserNotAuthenticated(ILogger logger);

    [LoggerMessage(
        EventId = EvtTenantIdClaimNotFound,
        Level = LogLevel.Debug,
        Message = "ClaimTenantIdentificationStrategy: Tenant ID claim '{ClaimType}' not found for authenticated user '{UserId}'.")]
    public static partial void LogTenantIdClaimNotFound(ILogger logger, string claimType, string userId);

    [LoggerMessage(
        EventId = EvtTenantIdClaimValueNullOrWhitespace,
        Level = LogLevel.Debug,
        Message = "ClaimTenantIdentificationStrategy: Tenant ID claim '{ClaimType}' found for user '{UserId}', but its value is null or whitespace.")]
    public static partial void LogTenantIdClaimValueNullOrWhitespace(ILogger logger, string claimType, string userId);

    [LoggerMessage(
        EventId = EvtTenantIdentifiedFromClaim,
        Level = LogLevel.Debug,
        Message = "ClaimTenantIdentificationStrategy: Identified potential tenant identifier '{TenantIdentifier}' from claim '{ClaimType}'.")]
    public static partial void LogTenantIdentifiedFromClaim(ILogger logger, string tenantIdentifier, string claimType);
}
