using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Interceptors;

public partial class LoggingInterceptor
{
    private const int ClassId = 10; 
    private const int BaseEventId = Logging.InfrastructureBaseId + (ClassId * Logging.IncrementPerClass);
    private const int EvtInterceptorInstantiated = BaseEventId + (0 * Logging.IncrementPerLog);
    private const int EvtMethodEntry = BaseEventId + (1 * Logging.IncrementPerLog);
    private const int EvtMethodExit = BaseEventId + (2 * Logging.IncrementPerLog);
    private const int EvtMethodException = BaseEventId + (3 * Logging.IncrementPerLog);
    private const int EvtMethodCanceled = BaseEventId + (4 * Logging.IncrementPerLog);
    private const int EvtSerializationError = BaseEventId + (5 * Logging.IncrementPerLog);


    [LoggerMessage(
        EventId = EvtInterceptorInstantiated,
        Level = LogLevel.Debug,
        Message = "[{InterceptorName}] Instantiated.")]
    static partial void LogInterceptorInstantiated(ILogger logger, string interceptorName);

    [LoggerMessage(
        EventId = EvtMethodEntry,
        Level = LogLevel.Information, // Consider Debug for less verbosity in production
        Message = "[{ClassName}.{MethodName}] ENTER. Arguments: [{Arguments}]")]
    static partial void LogMethodEntry(ILogger logger, string className, string methodName, string[] arguments);

    [LoggerMessage(
        EventId = EvtMethodExit,
        Level = LogLevel.Information, // Consider Debug
        Message = "[{ClassName}.{MethodName}] EXIT. Duration: {DurationMs}ms. ReturnValue: {ReturnValue}")]
    static partial void LogMethodExit(ILogger logger, string className, string methodName, long durationMs, string returnValue);

    [LoggerMessage(
        EventId = EvtMethodException,
        Level = LogLevel.Error,
        Message = "[{ClassName}.{MethodName}] EXCEPTION. Duration: {DurationMs}ms. ExceptionCount: {ExceptionCount}.")]
    static partial void LogMethodException(ILogger logger, string className, string methodName, long durationMs, int exceptionCount, Exception exception);

    [LoggerMessage(
        EventId = EvtMethodCanceled,
        Level = LogLevel.Warning,
        Message = "[{ClassName}.{MethodName}] CANCELED. Duration: {DurationMs}ms.")]
    static partial void LogMethodCanceled(ILogger logger, string className, string methodName, long durationMs);
    
    [LoggerMessage(
        EventId = EvtSerializationError,
        Level = LogLevel.Warning,
        Message = "[{InterceptorName}] Failed to serialize {ItemType} '{ItemName}'. Error: {ErrorMessage}."
    )]
    static partial void LogSerializationError(ILogger logger, string interceptorName, string itemType, string itemName, string errorMessage, Exception exception);
}