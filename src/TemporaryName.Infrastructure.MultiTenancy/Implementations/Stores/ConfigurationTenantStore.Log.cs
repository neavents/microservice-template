using System;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Stores;

public partial class ConfigurationTenantStore
{
    private const int ClassId = 35;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int EvtOptionsAccessorValueNull = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtStoreTypeMismatch = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtTenantsCollectionNull = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int EvtTenantsCollectionEmpty = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int EvtSkippingEntryNullIdentifier = BaseEventId + (4 * Logging.IncrementPerLog);
    public const int EvtSkippingEntryNullConfig = BaseEventId + (5 * Logging.IncrementPerLog);
    public const int EvtEntryMissingRequiredId = BaseEventId + (6 * Logging.IncrementPerLog);
    public const int EvtInvalidLogoUrl = BaseEventId + (7 * Logging.IncrementPerLog);
    public const int EvtDuplicateTenantIdentifier = BaseEventId + (8 * Logging.IncrementPerLog);
    public const int EvtTenantInfoCreationArgumentError = BaseEventId + (9 * Logging.IncrementPerLog);
    public const int EvtUnexpectedTenantEntryProcessingError = BaseEventId + (10 * Logging.IncrementPerLog);
    public const int EvtInitializationSuccess = BaseEventId + (11 * Logging.IncrementPerLog);
    public const int EvtGetTenantCalledWithNullOrEmptyId = BaseEventId + (12 * Logging.IncrementPerLog);
    public const int EvtTenantFoundByIdentifier = BaseEventId + (13 * Logging.IncrementPerLog);
    public const int EvtTenantNotFoundByIdentifier = BaseEventId + (14 * Logging.IncrementPerLog);

    [LoggerMessage(
        EventId = EvtOptionsAccessorValueNull,
        Level = LogLevel.Critical,
        Message = "IOptions<MultiTenancyOptions>.Value is null. MultiTenancy configuration is missing or malformed. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogOptionsAccessorValueNull(ILogger logger, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtStoreTypeMismatch,
        Level = LogLevel.Information,
        Message = "ConfigurationTenantStore initialized, but MultiTenancyOptions.Store.Type is '{StoreType}'. This store will not load tenants from configuration.")]
    public static partial void LogStoreTypeMismatch(ILogger logger, TenantStoreType storeType);

    [LoggerMessage(
        EventId = EvtTenantsCollectionNull,
        Level = LogLevel.Error,
        Message = "MultiTenancyOptions.Tenants collection is null, but Store.Type is '{StoreType}'. No tenants can be loaded. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogTenantsCollectionNull(ILogger logger, TenantStoreType storeType, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtTenantsCollectionEmpty,
        Level = LogLevel.Warning,
        Message = "MultiTenancyOptions.Tenants is empty. ConfigurationTenantStore will be initialized with no tenants.")]
    public static partial void LogTenantsCollectionEmpty(ILogger logger);

    [LoggerMessage(
        EventId = EvtSkippingEntryNullIdentifier,
        Level = LogLevel.Warning,
        Message = "Skipping tenant configuration entry: The identifier key is null or whitespace.")]
    public static partial void LogSkippingEntryNullIdentifier(ILogger logger);

    [LoggerMessage(
        EventId = EvtSkippingEntryNullConfig,
        Level = LogLevel.Warning,
        Message = "Skipping tenant configuration entry for identifier '{Identifier}': The configuration value is null.")]
    public static partial void LogSkippingEntryNullConfig(ILogger logger, string identifier);

    [LoggerMessage(
        EventId = EvtEntryMissingRequiredId,
        Level = LogLevel.Error,
        Message = "Tenant configuration entry for identifier '{Identifier}' is missing the required 'Id' property. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogEntryMissingRequiredId(ILogger logger, string identifier, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtInvalidLogoUrl,
        Level = LogLevel.Warning,
        Message = "Tenant '{TenantId}': Invalid or non-HTTP/HTTPS LogoUrl '{LogoUrl}'. It will be ignored.")]
    public static partial void LogInvalidLogoUrl(ILogger logger, string tenantId, string logoUrl);

    [LoggerMessage(
        EventId = EvtDuplicateTenantIdentifier,
        Level = LogLevel.Warning,
        Message = "Duplicate tenant identifier '{Identifier}' encountered in configuration. The first valid entry for this identifier was used. Subsequent entries are ignored.")]
    public static partial void LogDuplicateTenantIdentifier(ILogger logger, string identifier);

    [LoggerMessage(
        EventId = EvtTenantInfoCreationArgumentError,
        Level = LogLevel.Error,
        Message = "Failed to create TenantInfo object for identifier '{Identifier}' from configuration. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogTenantInfoCreationArgumentError(ILogger logger, string identifier, string errorCode, string? errorDescription, Exception ex);

    [LoggerMessage(
        EventId = EvtUnexpectedTenantEntryProcessingError,
        Level = LogLevel.Critical,
        Message = "An unexpected error occurred while processing tenant configuration for identifier '{Identifier}'. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogUnexpectedTenantEntryProcessingError(ILogger logger, string identifier, string errorCode, string? errorDescription, Exception ex);

    [LoggerMessage(
        EventId = EvtInitializationSuccess,
        Level = LogLevel.Information,
        Message = "ConfigurationTenantStore initialized successfully with {TenantCount} tenants from configuration.")]
    public static partial void LogInitializationSuccess(ILogger logger, int tenantCount);

    [LoggerMessage(
        EventId = EvtGetTenantCalledWithNullOrEmptyId,
        Level = LogLevel.Debug,
        Message = "GetTenantByIdentifierAsync called with null or empty identifier. Returning null.")]
    public static partial void LogGetTenantCalledWithNullOrEmptyId(ILogger logger);

    [LoggerMessage(
        EventId = EvtTenantFoundByIdentifier,
        Level = LogLevel.Debug,
        Message = "Tenant found for identifier '{Identifier}'. Tenant ID: '{TenantId}', Status: '{TenantStatus}'.")]
    public static partial void LogTenantFoundByIdentifier(ILogger logger, string identifier, string tenantId, TenantStatus tenantStatus);

    [LoggerMessage(
        EventId = EvtTenantNotFoundByIdentifier,
        Level = LogLevel.Debug,
        Message = "No tenant found in ConfigurationTenantStore for identifier '{Identifier}'.")]
    public static partial void LogTenantNotFoundByIdentifier(ILogger logger, string identifier);
}
