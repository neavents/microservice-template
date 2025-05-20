using System;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Providers;

public partial class TenantStrategyProvider
{
    private const int ClassId = 40;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int EvtAttemptingToGetStrategy = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtUnsupportedStrategyType = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtStrategyCreationFailedInvalidParams = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int EvtStrategyInstantiationFailed = BaseEventId + (3 * Logging.IncrementPerLog);
 
    [LoggerMessage(
        EventId = EvtAttemptingToGetStrategy,
        Level = LogLevel.Debug,
        Message = "Attempting to get strategy for type: {StrategyType}, ParameterName: '{ParameterName}', Order: {Order}")]
    public static partial void LogAttemptingToGetStrategy(ILogger logger, TenantResolutionStrategyType strategyType, string? parameterName, int order);

    [LoggerMessage(
        EventId = EvtUnsupportedStrategyType,
        Level = LogLevel.Error, // Matched to original LogError call
        Message = "Unsupported tenant resolution strategy type configured: {StrategyType}. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogUnsupportedStrategyType(ILogger logger, TenantResolutionStrategyType strategyType, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtStrategyCreationFailedInvalidParams,
        Level = LogLevel.Error,
        Message = "Failed to create tenant strategy due to invalid parameters for type {StrategyType}.")]
    public static partial void LogStrategyCreationFailedInvalidParams(ILogger logger, TenantResolutionStrategyType strategyType, Exception ex);

    [LoggerMessage(
        EventId = EvtStrategyInstantiationFailed,
        Level = LogLevel.Critical,
        Message = "Failed to instantiate tenant resolution strategy of type {StrategyType}. Error Code: {ErrorCode}, Details: {ErrorDescription}. See inner exception.")]
    public static partial void LogStrategyInstantiationFailed(ILogger logger, TenantResolutionStrategyType strategyType, string errorCode, string? errorDescription, Exception ex);
}

