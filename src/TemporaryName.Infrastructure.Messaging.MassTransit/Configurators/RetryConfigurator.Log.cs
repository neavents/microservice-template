using Microsoft.Extensions.Logging;
using System;
using TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators;

public static partial class RetryConfigurator
{
    private const int ClassId = 1;
    private const int BaseEventId = Logging.MassTransitBaseId + (ClassId * Logging.IncrementPerClass);
    private const int EvtMethodCalled = BaseEventId + (0 * Logging.IncrementPerLog);
    private const int EvtRetryOptionsMissing = BaseEventId + (1 * Logging.IncrementPerLog);
    private const int EvtRetryStrategyNone = BaseEventId + (2 * Logging.IncrementPerLog);
    private const int EvtRetryStrategyBeingConfigured = BaseEventId + (3 * Logging.IncrementPerLog);
    private const int EvtRetryHandlingExceptionType = BaseEventId + (4 * Logging.IncrementPerLog);
    private const int EvtRetryUnknownExceptionTypeToHandle = BaseEventId + (5 * Logging.IncrementPerLog);
    private const int EvtRetryHandlingAllNonIgnoredExceptions = BaseEventId + (6 * Logging.IncrementPerLog);
    private const int EvtRetryIgnoringExceptionType = BaseEventId + (7 * Logging.IncrementPerLog);
    private const int EvtRetryUnknownExceptionTypeToIgnore = BaseEventId + (8 * Logging.IncrementPerLog);
    private const int EvtRetryImmediateConfigured = BaseEventId + (9 * Logging.IncrementPerLog);
    private const int EvtRetryIntervalConfigured = BaseEventId + (10 * Logging.IncrementPerLog);
    private const int EvtRetryIncrementalConfigured = BaseEventId + (11 * Logging.IncrementPerLog);
    private const int EvtRetryExponentialConfigured = BaseEventId + (12 * Logging.IncrementPerLog);
    private const int EvtRetryPolicyApplied = BaseEventId + (13 * Logging.IncrementPerLog);
    private const int EvtRetryInvalidStrategy = BaseEventId + (14 * Logging.IncrementPerLog);
    private const int EvtExceptionTypeIgnoredByPolicy = BaseEventId + (15 * Logging.IncrementPerLog);
    private const int EvtRetryIntervalScheduleMissingOrEmpty = BaseEventId + (16 * Logging.IncrementPerLog);


    [LoggerMessage(EventId = EvtMethodCalled, Level = LogLevel.Debug, Message = "{LogPrefix}Method called: {MethodName}.")]
    private static partial void LogMethodCalled(ILogger logger, string logPrefix, string methodName);

    [LoggerMessage(EventId = EvtRetryOptionsMissing, Level = LogLevel.Warning, Message = "RetryConfigurator ({TransportName}): ConsumerRetryOptions not provided. MassTransit default retry behavior will apply (if any).")]
    public static partial void LogRetryOptionsMissing(ILogger logger, string transportName);

    [LoggerMessage(EventId = EvtRetryStrategyNone, Level = LogLevel.Information, Message = "RetryConfigurator ({TransportName}): Retry strategy set to 'None'. No retries will be performed by this configuration.")]
    public static partial void LogRetryStrategyNone(ILogger logger, string transportName);

    [LoggerMessage(EventId = EvtRetryStrategyBeingConfigured, Level = LogLevel.Information, Message = "RetryConfigurator ({TransportName}): Configuring consumer retry strategy '{Strategy}' with limit {RetryLimit}.")]
    public static partial void LogRetryStrategyBeingConfigured(ILogger logger, RetryStrategy strategy, int retryLimit, string transportName);

    [LoggerMessage(EventId = EvtRetryHandlingExceptionType, Level = LogLevel.Debug, Message = "RetryConfigurator ({TransportName}): Retry policy will handle exception type: {ExceptionType}.")]
    public static partial void LogRetryHandlingExceptionType(ILogger logger, string exceptionType, string transportName);

    [LoggerMessage(EventId = EvtRetryUnknownExceptionTypeToHandle, Level = LogLevel.Warning, Message = "RetryConfigurator ({TransportName}): Unknown exception type '{ExceptionTypeName}' specified in HandleExceptionTypes. It will be ignored.")]
    public static partial void LogRetryUnknownExceptionTypeToHandle(ILogger logger, string exceptionTypeName, string transportName);

    [LoggerMessage(EventId = EvtRetryHandlingAllNonIgnoredExceptions, Level = LogLevel.Debug, Message = "RetryConfigurator ({TransportName}): Retry policy will handle all exceptions not explicitly ignored, as no specific HandleExceptionTypes were provided.")]
    public static partial void LogRetryHandlingAllNonIgnoredExceptions(ILogger logger, string transportName);

    [LoggerMessage(EventId = EvtRetryIgnoringExceptionType, Level = LogLevel.Debug, Message = "RetryConfigurator ({TransportName}): Retry policy will ignore exception type: {ExceptionType}.")]
    public static partial void LogRetryIgnoringExceptionType(ILogger logger, string exceptionType, string transportName);

    [LoggerMessage(EventId = EvtRetryUnknownExceptionTypeToIgnore, Level = LogLevel.Warning, Message = "RetryConfigurator ({TransportName}): Unknown exception type '{ExceptionTypeName}' specified in IgnoreExceptionTypes. It will be ignored.")]
    public static partial void LogRetryUnknownExceptionTypeToIgnore(ILogger logger, string exceptionTypeName, string transportName);

    [LoggerMessage(EventId = EvtRetryImmediateConfigured, Level = LogLevel.Debug, Message = "RetryConfigurator ({TransportName}): Immediate retry policy configured: Limit={Limit}.")]
    public static partial void LogRetryImmediateConfigured(ILogger logger, int limit, string transportName);

    [LoggerMessage(EventId = EvtRetryIntervalConfigured, Level = LogLevel.Debug, Message = "RetryConfigurator ({TransportName}): Interval retry policy configured: Limit={Limit}, IntervalsMs=[{IntervalsMs}].")]
    public static partial void LogRetryIntervalConfigured(ILogger logger, int limit, string intervalsMs, string transportName);

     [LoggerMessage(EventId = EvtRetryIntervalScheduleMissingOrEmpty, Level = LogLevel.Warning, Message = "RetryConfigurator ({TransportName}): Interval retry strategy specified, but IntervalScheduleMs is missing, empty, or invalid. Defaulting to no retry for this policy.")]
    public static partial void LogRetryIntervalScheduleMissingOrEmpty(ILogger logger, string transportName);

    [LoggerMessage(EventId = EvtRetryIncrementalConfigured, Level = LogLevel.Debug, Message = "RetryConfigurator ({TransportName}): Incremental retry policy configured: Limit={Limit}, InitialIntervalMs={InitialMs}, IncrementMs={IncrementMs}.")]
    public static partial void LogRetryIncrementalConfigured(ILogger logger, int limit, int initialMs, int incrementMs, string transportName);

    [LoggerMessage(EventId = EvtRetryExponentialConfigured, Level = LogLevel.Debug, Message = "RetryConfigurator ({TransportName}): Exponential retry policy configured: Limit={Limit}, MinIntervalMs={MinMs}, MaxIntervalMs={MaxMs}, FactorIntervalDeltaMs={FactorMs}.")]
    public static partial void LogRetryExponentialConfigured(ILogger logger, int limit, int minMs, int maxMs, double factorMs, string transportName);

    [LoggerMessage(EventId = EvtRetryPolicyApplied, Level = LogLevel.Information, Message = "RetryConfigurator ({TransportName}): Consumer retry policy '{Strategy}' applied successfully.")]
    public static partial void LogRetryPolicyApplied(ILogger logger, RetryStrategy strategy, string transportName);

    [LoggerMessage(EventId = EvtRetryInvalidStrategy, Level = LogLevel.Error, Message = "RetryConfigurator ({TransportName}): Invalid retry strategy '{StrategyName}' encountered. Defaulting to no retry.")]
    public static partial void LogRetryInvalidStrategy(ILogger logger, string strategyName, string transportName);

    [LoggerMessage(EventId = EvtExceptionTypeIgnoredByPolicy, Level = LogLevel.Debug, Message = "RetryConfigurator ({TransportName}): Exception type '{ThrownExceptionType}' will be ignored for retries based on configured ignore rule for '{IgnoredRuleType}'.")]
    public static partial void LogExceptionTypeIgnoredByPolicy(ILogger logger, string thrownExceptionType, string ignoredRuleType, string transportName);
}