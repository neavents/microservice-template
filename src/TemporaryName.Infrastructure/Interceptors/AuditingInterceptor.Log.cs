using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Interceptors;

public partial class AuditingInterceptor
{
    private const int ClassId = 2;
    private const int BaseEventId = Logging.InfrastructureBaseId + (ClassId * Logging.IncrementPerClass);
    private const int EvtInterceptorInstantiated = BaseEventId + (0 * Logging.IncrementPerLog);
    private const int EvtAuditAttempt = BaseEventId + (1 * Logging.IncrementPerLog);
    private const int EvtAuditSuccess = BaseEventId + (2 * Logging.IncrementPerLog);
    private const int EvtAuditFailure = BaseEventId + (3 * Logging.IncrementPerLog);
    private const int EvtAuditCanceled = BaseEventId + (4 * Logging.IncrementPerLog);
    private const int EvtAuditEntryPersisted = BaseEventId + (5 * Logging.IncrementPerLog);
    private const int EvtSerializationError = BaseEventId + (6 * Logging.IncrementPerLog);
    private const int EvtAuditWarning = BaseEventId + (7 * Logging.IncrementPerLog);


    [LoggerMessage(
        EventId = EvtInterceptorInstantiated,
        Level = LogLevel.Debug,
        Message = "[{InterceptorName}] Instantiated.")]
    static partial void LogInterceptorInstantiated(ILogger logger, string interceptorName);

    [LoggerMessage(
        EventId = EvtAuditAttempt,
        Level = LogLevel.Information,
        Message = "[AUDIT ATTEMPT][{ClassName}.{MethodName}] User: '{UserId}', IP: {ClientIpAddress}")]
    static partial void LogAuditAttempt(ILogger logger, string className, string methodName, string userId, string clientIpAddress);

    [LoggerMessage(
        EventId = EvtAuditSuccess,
        Level = LogLevel.Information,
        Message = "[AUDIT SUCCESS][{ClassName}.{MethodName}] User: '{UserId}'. Duration: {DurationMs}ms.")]
    static partial void LogAuditSuccess(ILogger logger, string className, string methodName, string userId, long durationMs);

    [LoggerMessage(
        EventId = EvtAuditFailure,
        Level = LogLevel.Warning, // Audit failures are warnings; the actual exception is logged as Error by LoggingInterceptor
        Message = "[AUDIT FAILURE][{ClassName}.{MethodName}] User: '{UserId}'. Duration: {DurationMs}ms. ExceptionType: {ExceptionType}, Message: {ExceptionMessage}")]
    static partial void LogAuditFailure(ILogger logger, string className, string methodName, string userId, long durationMs, string exceptionType, string exceptionMessage, Exception? ex = null);
    
    [LoggerMessage(
        EventId = EvtAuditCanceled,
        Level = LogLevel.Warning,
        Message = "[AUDIT CANCELED][{ClassName}.{MethodName}] User: '{UserId}'. Duration: {DurationMs}ms.")]
    static partial void LogAuditCanceled(ILogger logger, string className, string methodName, string userId, long durationMs);

    [LoggerMessage(
        EventId = EvtAuditEntryPersisted, // In a real scenario, this confirms DB write
        Level = LogLevel.Debug, // More detailed, confirming the audit "write"
        Message = "[AUDIT PERSISTED][{ClassName}.{MethodName}] User: '{UserId}', Success: {Success}, Duration: {DurationMs}ms, ParamsLen: {ParamsLength}, ReturnLen: {ReturnLength}, Exception: {ExceptionType}")]
    static partial void LogAuditEntryPersisted(ILogger logger, string className, string methodName, string userId, bool success, double durationMs, int paramsLength, int returnLength, string exceptionType);

    [LoggerMessage(
        EventId = EvtSerializationError,
        Level = LogLevel.Error, // Changed to Error as this is a failure within the interceptor
        Message = "[{InterceptorName}] Failed to serialize {ItemType} for audit '{ItemName}'. Error: {ErrorMessage}."
    )]
    static partial void LogSerializationError(ILogger logger, string interceptorName, string itemType, string itemName, string errorMessage, Exception exception);

    [LoggerMessage(
        EventId = EvtAuditWarning,
        Level = LogLevel.Warning,
        Message = "[AUDIT WARNING][{ClassName}.{MethodName}] User: '{UserId}'. Details: {WarningDetails}."
    )]
    static partial void LogAuditWarning(ILogger logger, string className, string methodName, string userId, string warningDetails, Exception? ex = null);

}