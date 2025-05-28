using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;

namespace TemporaryName.Infrastructure.MultiTenancy.Middlewares;

public partial class TenantResolutionMiddleware
{
    private const int ClassId = 90;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int EvtOptionsNull = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtNoResolutionStrategiesConfigured = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtStoreOptionsNull = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int EvtMultiTenancyDisabled = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int EvtResolutionProcessStarted = BaseEventId + (4 * Logging.IncrementPerLog);
    public const int EvtAttemptingStrategy = BaseEventId + (5 * Logging.IncrementPerLog);
    public const int EvtIdentifierFoundByStrategy = BaseEventId + (6 * Logging.IncrementPerLog);
    public const int EvtStrategyDidNotYieldIdentifier = BaseEventId + (7 * Logging.IncrementPerLog);
    public const int EvtAttemptingHostMapping = BaseEventId + (8 * Logging.IncrementPerLog);
    public const int EvtUsingDefaultTenantIdentifier = BaseEventId + (9 * Logging.IncrementPerLog);
    public const int EvtFetchingTenantInfoFromStore = BaseEventId + (10 * Logging.IncrementPerLog);
    public const int EvtTenantNotFoundInStore = BaseEventId + (11 * Logging.IncrementPerLog);
    public const int EvtMisconfiguredDefaultOrMappedNotFound = BaseEventId + (12 * Logging.IncrementPerLog);
    public const int EvtTenantResolvedSuccessfully = BaseEventId + (13 * Logging.IncrementPerLog);
    public const int EvtNoIdentifierAllowUnresolved = BaseEventId + (14 * Logging.IncrementPerLog);
    public const int EvtNoIdentifierResolutionFailedRequired = BaseEventId + (15 * Logging.IncrementPerLog);
    public const int EvtNoIdentifierProceedNullContext = BaseEventId + (16 * Logging.IncrementPerLog);
    public const int EvtResolvedTenantNotActive = BaseEventId + (17 * Logging.IncrementPerLog);
    public const int EvtApplyingDefaultSettings = BaseEventId + (18 * Logging.IncrementPerLog);
    public const int EvtResolutionCompleteTenantResolved = BaseEventId + (19 * Logging.IncrementPerLog);
    public const int EvtResolutionCompleteNoTenant = BaseEventId + (20 * Logging.IncrementPerLog);
    public const int EvtTenantResolutionExceptionCaught = BaseEventId + (21 * Logging.IncrementPerLog);
    public const int EvtTenantConfigurationExceptionCaught = BaseEventId + (22 * Logging.IncrementPerLog);
    public const int EvtTenantStoreExceptionCaught = BaseEventId + (23 * Logging.IncrementPerLog);
    public const int EvtTenantCacheExceptionCaught = BaseEventId + (24 * Logging.IncrementPerLog);
    public const int EvtMiddlewareUnexpectedError = BaseEventId + (25 * Logging.IncrementPerLog);


    [LoggerMessage(
        EventId = EvtOptionsNull,
        Level = LogLevel.Critical,
        Message = "MultiTenancyOptions resolved to null. Middleware cannot operate. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogOptionsNull(ILogger logger, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtNoResolutionStrategiesConfigured,
        Level = LogLevel.Warning,
        Message = "MultiTenancy is enabled, but no resolution strategies are configured, no default tenant identifier is set, and no host mapping is defined. Tenant resolution will likely fail for all requests.")]
    public static partial void LogNoResolutionStrategiesConfigured(ILogger logger);

    [LoggerMessage(
        EventId = EvtStoreOptionsNull,
        Level = LogLevel.Critical,
        Message = "MultiTenancyOptions.Store is null. Middleware cannot determine how to fetch tenants. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogStoreOptionsNull(ILogger logger, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtMultiTenancyDisabled,
        Level = LogLevel.Debug,
        Message = "MultiTenancy is disabled. Skipping tenant resolution.")]
    public static partial void LogMultiTenancyDisabled(ILogger logger);

    [LoggerMessage(
        EventId = EvtResolutionProcessStarted,
        Level = LogLevel.Debug,
        Message = "Tenant resolution process started for request path: {RequestPath}")]
    public static partial void LogResolutionProcessStarted(ILogger logger, PathString requestPath);

    [LoggerMessage(
        EventId = EvtAttemptingStrategy,
        Level = LogLevel.Debug,
        Message = "Attempting strategy: {StrategyType} (Order: {Order})")]
    public static partial void LogAttemptingStrategy(ILogger logger, TenantResolutionStrategyType strategyType, int order);

    [LoggerMessage(
        EventId = EvtIdentifierFoundByStrategy,
        Level = LogLevel.Information,
        Message = "Tenant identifier '{TenantIdentifier}' found using strategy {StrategyType}.")]
    public static partial void LogIdentifierFoundByStrategy(ILogger logger, string tenantIdentifier, TenantResolutionStrategyType strategyType);

    [LoggerMessage(
        EventId = EvtStrategyDidNotYieldIdentifier,
        Level = LogLevel.Debug,
        Message = "Strategy {StrategyType} did not yield an identifier.")]
    public static partial void LogStrategyDidNotYieldIdentifier(ILogger logger, TenantResolutionStrategyType strategyType);

    [LoggerMessage(
        EventId = EvtAttemptingHostMapping,
        Level = LogLevel.Information,
        Message = "No tenant identified by strategies. Attempting to map host request to tenant identifier: '{MapToTenantIdentifier}'.")]
    public static partial void LogAttemptingHostMapping(ILogger logger, string? mapToTenantIdentifier);

    [LoggerMessage(
        EventId = EvtUsingDefaultTenantIdentifier,
        Level = LogLevel.Information,
        Message = "No tenant identifier from strategies or host mapping. Using DefaultTenantIdentifier: '{DefaultTenantIdentifier}'.")]
    public static partial void LogUsingDefaultTenantIdentifier(ILogger logger, string? defaultTenantIdentifier);

    [LoggerMessage(
        EventId = EvtFetchingTenantInfoFromStore,
        Level = LogLevel.Debug,
        Message = "Attempting to fetch tenant info for identifier '{TenantIdentifier}' from store type {StoreType}.")]
    public static partial void LogFetchingTenantInfoFromStore(ILogger logger, string tenantIdentifier, TenantStoreType storeType);

    [LoggerMessage(
        EventId = EvtTenantNotFoundInStore,
        Level = LogLevel.Warning,
        Message = "Tenant with identifier '{TenantIdentifier}' not found in the configured store ({StoreType}). Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogTenantNotFoundInStore(ILogger logger, string tenantIdentifier, TenantStoreType storeType, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtMisconfiguredDefaultOrMappedNotFound,
        Level = LogLevel.Critical,
        Message = "The configured DefaultTenantIdentifier or HostHandling.MapToTenantIdentifier '{TenantIdentifier}' was not found in the store. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogMisconfiguredDefaultOrMappedNotFound(ILogger logger, string tenantIdentifier, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtTenantResolvedSuccessfully,
        Level = LogLevel.Information,
        Message = "Tenant '{TenantId}' (Identifier: '{TenantIdentifier}') resolved successfully from store. Status: {TenantStatus}")]
    public static partial void LogTenantResolvedSuccessfully(ILogger logger, string tenantId, string tenantIdentifier, TenantStatus tenantStatus);

    [LoggerMessage(
        EventId = EvtNoIdentifierAllowUnresolved,
        Level = LogLevel.Information,
        Message = "No tenant identifier resolved, and HostHandling.AllowUnresolvedRequests is true. Proceeding with a null tenant context.")]
    public static partial void LogNoIdentifierAllowUnresolved(ILogger logger);

    [LoggerMessage(
        EventId = EvtNoIdentifierResolutionFailedRequired,
        Level = LogLevel.Warning,
        Message = "Tenant could not be identified from the request, and no default or host mapping was applicable. Tenant identification is required. Request Path: {RequestPath}. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogNoIdentifierResolutionFailedRequired(ILogger logger, PathString requestPath, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtNoIdentifierProceedNullContext,
        Level = LogLevel.Information,
        Message = "No tenant identifier resolved, and ThrowIfTenantMissing is false. Proceeding with a null tenant context.")]
    public static partial void LogNoIdentifierProceedNullContext(ILogger logger);

    [LoggerMessage(
        EventId = EvtResolvedTenantNotActive,
        Level = LogLevel.Warning,
        Message = "Resolved tenant '{TenantId}' is not active. Status: {TenantStatus}.")]
    public static partial void LogResolvedTenantNotActive(ILogger logger, string tenantId, TenantStatus tenantStatus);

    [LoggerMessage(
        EventId = EvtApplyingDefaultSettings,
        Level = LogLevel.Debug,
        Message = "Applying default settings to tenant '{TenantId}'. Original: Locale={OriginalLocale}, TZ={OriginalTimeZoneId}, Region={OriginalDataRegion}, Tier={OriginalSubscriptionTier}, Isolation={OriginalDataIsolationMode}. Defaults: Locale={DefaultLocale}, TZ={DefaultTimeZoneId}, Region={DefaultDataRegion}, Tier={DefaultSubscriptionTier}, Isolation={DefaultDataIsolationMode}")]
    public static partial void LogApplyingDefaultSettings(ILogger logger,
        string tenantId,
        string? originalLocale, string? originalTimeZoneId, string? originalDataRegion, string? originalSubscriptionTier, TenantDataIsolationMode originalDataIsolationMode,
        string? defaultLocale, string? defaultTimeZoneId, string? defaultDataRegion, string? defaultSubscriptionTier, TenantDataIsolationMode? defaultDataIsolationMode);

    [LoggerMessage(
        EventId = EvtResolutionCompleteTenantResolved,
        Level = LogLevel.Information,
        Message = "Tenant resolution complete. Current Tenant ID: '{TenantId}', Status: {TenantStatus}. Proceeding with request.")]
    public static partial void LogResolutionCompleteTenantResolved(ILogger logger, string tenantId, TenantStatus tenantStatus);

    [LoggerMessage(
        EventId = EvtResolutionCompleteNoTenant,
        Level = LogLevel.Information,
        Message = "Tenant resolution complete. No tenant resolved. Proceeding with request (null tenant context).")]
    public static partial void LogResolutionCompleteNoTenant(ILogger logger);

    [LoggerMessage(
        EventId = EvtTenantResolutionExceptionCaught,
        Level = LogLevel.Error,
        Message = "TenantResolutionException caught by middleware: {ErrorMessage}. Tenant resolution failed.")]
    public static partial void LogTenantResolutionExceptionCaught(ILogger logger, string errorMessage, Exception ex);

    [LoggerMessage(
        EventId = EvtTenantConfigurationExceptionCaught,
        Level = LogLevel.Critical,
        Message = "TenantConfigurationException caught by middleware: {ErrorMessage}. This indicates a severe misconfiguration.")]
    public static partial void LogTenantConfigurationExceptionCaught(ILogger logger, string errorMessage, Exception ex);

    [LoggerMessage(
        EventId = EvtTenantStoreExceptionCaught,
        Level = LogLevel.Error,
        Message = "TenantStoreException caught by middleware: {ErrorMessage}. Failed to retrieve tenant from store.")]
    public static partial void LogTenantStoreExceptionCaught(ILogger logger, string errorMessage, Exception ex);

    [LoggerMessage(
        EventId = EvtTenantCacheExceptionCaught,
        Level = LogLevel.Error,
        Message = "TenantCacheException caught by middleware: {ErrorMessage}. Error interacting with tenant cache.")]
    public static partial void LogTenantCacheExceptionCaught(ILogger logger, string errorMessage, Exception ex);

    [LoggerMessage(
        EventId = EvtMiddlewareUnexpectedError,
        Level = LogLevel.Critical,
        Message = "An unexpected error occurred during tenant resolution. Request Path: {RequestPath}. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogMiddlewareUnexpectedError(ILogger logger, PathString requestPath, string errorCode, string? errorDescription, Exception ex);
}
