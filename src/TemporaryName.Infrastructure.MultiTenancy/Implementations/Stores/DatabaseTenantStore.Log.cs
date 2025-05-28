using System;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Stores;

public partial class DatabaseTenantStore
{
    private const int ClassId = 40;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int EvtOptionsAccessorValueNull = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtStoreTypeMismatch = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtMissingConnectionStringName = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int EvtInitializationSuccess = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int EvtGetTenantCalledWithNullOrEmptyId = BaseEventId + (4 * Logging.IncrementPerLog);
    public const int EvtNoTenantFoundInDbByIdentifier = BaseEventId + (5 * Logging.IncrementPerLog);
    public const int EvtInvalidLogoUrl = BaseEventId + (6 * Logging.IncrementPerLog);
    public const int EvtDeserializeEnabledFeaturesJsonFailed = BaseEventId + (7 * Logging.IncrementPerLog);
    public const int EvtDeserializeCustomPropertiesJsonFailed = BaseEventId + (8 * Logging.IncrementPerLog);
    public const int EvtTenantFoundInDbByIdentifier = BaseEventId + (9 * Logging.IncrementPerLog);
    public const int EvtDbConfigErrorConnectionStringNotFound = BaseEventId + (10 * Logging.IncrementPerLog);
    public const int EvtDbConfigErrorUnsupportedProvider = BaseEventId + (11 * Logging.IncrementPerLog);
    public const int EvtDbUnavailableConnectionOpenFailed = BaseEventId + (12 * Logging.IncrementPerLog);
    public const int EvtDbQueryFailed = BaseEventId + (13 * Logging.IncrementPerLog);
    public const int EvtDbDeserializationFailed = BaseEventId + (14 * Logging.IncrementPerLog);
    public const int EvtDbUnexpectedError = BaseEventId + (15 * Logging.IncrementPerLog);

    [LoggerMessage(
        EventId = EvtOptionsAccessorValueNull,
        Level = LogLevel.Critical,
        Message = "IOptions<MultiTenancyOptions>.Value is null. DatabaseTenantStore cannot be initialized. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogOptionsAccessorValueNull(ILogger logger, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtStoreTypeMismatch,
        Level = LogLevel.Warning,
        Message = "DatabaseTenantStore is registered, but MultiTenancyOptions.Store.Type is '{StoreType}'. This store might not be used as intended by the configuration.")]
    public static partial void LogStoreTypeMismatch(ILogger logger, TenantStoreType storeType);

    [LoggerMessage(
        EventId = EvtMissingConnectionStringName,
        Level = LogLevel.Critical,
        Message = "DatabaseTenantStore requires MultiTenancyOptions.Store.ConnectionStringName to be configured for the tenant metadata database. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogMissingConnectionStringName(ILogger logger, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtInitializationSuccess,
        Level = LogLevel.Information,
        Message = "DatabaseTenantStore initialized. Will use connection string name '{ConnectionStringName}' for tenant metadata via IDbConnectionFactory.")]
    public static partial void LogInitializationSuccess(ILogger logger, string? connectionStringName);

    [LoggerMessage(
        EventId = EvtGetTenantCalledWithNullOrEmptyId,
        Level = LogLevel.Debug,
        Message = "DatabaseTenantStore.GetTenantByIdentifierAsync called with null or empty identifier.")]
    public static partial void LogGetTenantCalledWithNullOrEmptyId(ILogger logger);

    [LoggerMessage(
        EventId = EvtNoTenantFoundInDbByIdentifier,
        Level = LogLevel.Debug,
        Message = "No tenant found in database for identifier '{Identifier}'.")]
    public static partial void LogNoTenantFoundInDbByIdentifier(ILogger logger, string identifier);

    [LoggerMessage(
        EventId = EvtInvalidLogoUrl,
        Level = LogLevel.Warning,
        Message = "Tenant '{TenantId}' from DB: Invalid or non-HTTP/HTTPS LogoUrl '{LogoUrl}'. It will be ignored.")]
    public static partial void LogInvalidLogoUrl(ILogger logger, string tenantId, string? logoUrl);

    [LoggerMessage(
        EventId = EvtDeserializeEnabledFeaturesJsonFailed,
        Level = LogLevel.Warning,
        Message = "Tenant '{TenantId}' from DB: Failed to deserialize EnabledFeaturesJson. Value: '{JsonValue}'")]
    public static partial void LogDeserializeEnabledFeaturesJsonFailed(ILogger logger, string tenantId, string? jsonValue, Exception ex);

    [LoggerMessage(
        EventId = EvtDeserializeCustomPropertiesJsonFailed,
        Level = LogLevel.Warning,
        Message = "Tenant '{TenantId}' from DB: Failed to deserialize CustomPropertiesJson. Value: '{JsonValue}'")]
    public static partial void LogDeserializeCustomPropertiesJsonFailed(ILogger logger, string tenantId, string? jsonValue, Exception ex);

    [LoggerMessage(
        EventId = EvtTenantFoundInDbByIdentifier,
        Level = LogLevel.Debug,
        Message = "Tenant found in database for identifier '{Identifier}'. Tenant ID: '{TenantId}', Status: '{TenantStatus}'.")]
    public static partial void LogTenantFoundInDbByIdentifier(ILogger logger, string identifier, string tenantId, TenantStatus tenantStatus);

    [LoggerMessage(
        EventId = EvtDbConfigErrorConnectionStringNotFound,
        Level = LogLevel.Critical,
        Message = "Configuration error for tenant metadata database: {ExceptionMessage}. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogDbConfigErrorConnectionStringNotFound(ILogger logger, string exceptionMessage, string errorCode, string? errorDescription, Exception ex);

    [LoggerMessage(
        EventId = EvtDbConfigErrorUnsupportedProvider,
        Level = LogLevel.Critical,
        Message = "Unsupported DB provider for tenant metadata database: {ExceptionMessage}. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogDbConfigErrorUnsupportedProvider(ILogger logger, string exceptionMessage, string errorCode, string? errorDescription, Exception ex);

    [LoggerMessage(
        EventId = EvtDbUnavailableConnectionOpenFailed,
        Level = LogLevel.Critical,
        Message = "Tenant metadata database is unavailable: {ExceptionMessage}. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogDbUnavailableConnectionOpenFailed(ILogger logger, string exceptionMessage, string errorCode, string? errorDescription, Exception ex);

    [LoggerMessage(
        EventId = EvtDbQueryFailed,
        Level = LogLevel.Error,
        Message = "Database query failed while retrieving tenant by identifier '{Identifier}'. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogDbQueryFailed(ILogger logger, string identifier, string errorCode, string? errorDescription, Exception ex);

    [LoggerMessage(
        EventId = EvtDbDeserializationFailed,
        Level = LogLevel.Error,
        Message = "Failed to deserialize tenant data for identifier '{Identifier}' from database response. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogDbDeserializationFailed(ILogger logger, string identifier, string errorCode, string? errorDescription, Exception ex);

    [LoggerMessage(
        EventId = EvtDbUnexpectedError,
        Level = LogLevel.Error,
        Message = "An unexpected error occurred in DatabaseTenantStore while retrieving tenant by identifier '{Identifier}'. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogDbUnexpectedError(ILogger logger, string identifier, string errorCode, string? errorDescription, Exception ex);

}
