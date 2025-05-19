using System;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Providers;

public partial class TenantStoreProvider
{

    private const int ClassId = 30;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int EvtGettingBaseStoreInfo = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtCustomStoreNotDistinctlyRegistered = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtUsingCustomStore = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int EvtUnsupportedStoreType = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int EvtStoreCreationConfigError = BaseEventId + (4 * Logging.IncrementPerLog);
    public const int EvtStoreInstantiationFailed = BaseEventId + (5 * Logging.IncrementPerLog);
    public const int EvtBaseStoreCreated = BaseEventId + (6 * Logging.IncrementPerLog);

    [LoggerMessage(
        EventId = EvtGettingBaseStoreInfo,
        Level = LogLevel.Debug,
        Message = "TenantStoreProvider: Getting base store for type: {StoreType}, ConnectionStringName: '{ConnectionStringName}', ServiceEndpoint: '{ServiceEndpoint}'")]
    public static partial void LogGettingBaseStoreInfo(ILogger logger, TenantStoreType storeType, string? connectionStringName, string? serviceEndpoint);

    [LoggerMessage(
        EventId = EvtCustomStoreNotDistinctlyRegistered,
        Level = LogLevel.Critical,
        Message = "Custom store not distinctly registered. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogCustomStoreNotDistinctlyRegistered(ILogger logger, string errorCode, string errorDescription);

    [LoggerMessage(
        EventId = EvtUsingCustomStore,
        Level = LogLevel.Information,
        Message = "TenantStoreProvider: Using custom registered ITenantStore of type {CustomStoreType}")]
    public static partial void LogUsingCustomStore(ILogger logger, string customStoreType);

    [LoggerMessage(
        EventId = EvtUnsupportedStoreType,
        Level = LogLevel.Critical,
        Message = "Unsupported tenant store type configured: {StoreType}. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogUnsupportedStoreType(ILogger logger, string storeType, string errorCode, string errorDescription);

    [LoggerMessage(
        EventId = EvtStoreCreationConfigError,
        Level = LogLevel.Error,
        Message = "TenantStoreProvider: Failed to create tenant store due to configuration issues for type {StoreType}.")]
    public static partial void LogStoreCreationConfigError(ILogger logger, TenantStoreType storeType, Exception ex);

    [LoggerMessage(
        EventId = EvtStoreInstantiationFailed,
        Level = LogLevel.Critical,
        Message = "TenantStoreProvider: Failed to instantiate tenant store of type {StoreType}. Error Code: {ErrorCode}, Details: {ErrorDescription}. See inner exception for details.")]
    public static partial void LogStoreInstantiationFailed(ILogger logger, string storeType, string errorCode, string errorDescription, Exception ex);

    [LoggerMessage(
        EventId = EvtBaseStoreCreated,
        Level = LogLevel.Information,
        Message = "TenantStoreProvider: Successfully created base store of type {StoreTypeResolved}.")]
    public static partial void LogBaseStoreCreated(ILogger logger, string storeTypeResolved);
}
