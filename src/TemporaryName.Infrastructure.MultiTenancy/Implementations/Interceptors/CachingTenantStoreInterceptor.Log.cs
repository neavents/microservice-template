using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Interceptors;

public partial class CachingTenantStoreInterceptor
{
    private const int ClassId = 20;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int EvtInterceptorInstantiatedButCacheIsDisabled = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtStrangeCacheExpirationAndSliding = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtProceedingWithoutCaching = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int EvtIntercepting = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int EvtTenantCacheHit = BaseEventId + (4 * Logging.IncrementPerLog);
    public const int EvtTenantCacheMiss = BaseEventId + (5 * Logging.IncrementPerLog);
    public const int EvtUnderlyingCallFailed = BaseEventId + (6 * Logging.IncrementPerLog);
    public const int EvtUnderlyingCallCancelled = BaseEventId + (7 * Logging.IncrementPerLog);
    public const int EvtCanNotAccessResultOfCompletedTaskForCaching = BaseEventId + (8 * Logging.IncrementPerLog);
    public const int EvtCanNotExtractResultForCaching = BaseEventId + (9 * Logging.IncrementPerLog);
    public const int EvtCachingTheResult = BaseEventId + (10 * Logging.IncrementPerLog);
    public const int EvtFetchedButNotCachedBecauseExpiration = BaseEventId + (11 * Logging.IncrementPerLog);
    public const int EvtInterceptedButNotReturnedTask = BaseEventId + (12 * Logging.IncrementPerLog);
    public const int EvtCriteriaOrArgumentMisconfiguration = BaseEventId + (13 * Logging.IncrementPerLog);


    [LoggerMessage(
        EventId = EvtInterceptorInstantiatedButCacheIsDisabled,
        Level = LogLevel.Warning,
        Message = "CachingTenantStoreInterceptor instantiated, but CacheOptions.Enabled is false. Interception will likely bypass caching.")]
    public static partial void LogInterceptorInstantiatedButCacheIsDisabled(ILogger logger);

    [LoggerMessage(
        EventId = EvtStrangeCacheExpirationAndSliding,
        Level = LogLevel.Warning,
        Message = "CachingTenantStoreInterceptor: Cache is enabled but both AbsoluteExpirationSeconds and SlidingExpirationSeconds are non-positive. Items might not be cached effectively.")]
    public static partial void LogStrangeCacheExpirationAndSliding(ILogger logger);

    [LoggerMessage(
        EventId = EvtProceedingWithoutCaching,
        Level = LogLevel.Trace,
        Message = "Caching is disabled, proceeding with invocation for {MethodName} without caching.")]
    public static partial void LogProceedingWithoutCaching(ILogger logger, string methodName);

    [LoggerMessage(
        EventId = EvtIntercepting,
        Level = LogLevel.Trace,
        Message = "Intercepting {MethodName} for identifier '{Identifier}'. Cache key: '{CacheKey}'.")]
    public static partial void LogIntercepting(ILogger logger, string methodName, string identifier, string cacheKey);

    [LoggerMessage(
        EventId = EvtTenantCacheHit,
        Level = LogLevel.Debug,
        Message = "Tenant '{Identifier}' found in cache for method {MethodName}. Cache key: '{CacheKey}'.")]
    public static partial void LogTenantCacheHit(ILogger logger, string identifier, string methodName, string cacheKey);

    [LoggerMessage(
        EventId = EvtTenantCacheMiss,
        Level = LogLevel.Debug,
        Message = "Tenant '{Identifier}' not found in cache for method {MethodName}. Proceeding with actual call. Cache key: '{CacheKey}'.")]
    public static partial void LogTenantCacheMiss(ILogger logger, string identifier, string methodName, string cacheKey);

    [LoggerMessage(
        EventId = EvtUnderlyingCallFailed,
        Level = LogLevel.Warning,
        Message = "Underlying call for {MethodName} (identifier: {Identifier}, cache key: {CacheKey}) failed. Not caching faulted task.")]
    public static partial void LogUnderlyingCallFailed(ILogger logger, string methodName, string identifier, string cacheKey, Exception? exception);

    [LoggerMessage(
        EventId = EvtUnderlyingCallCancelled,
        Level = LogLevel.Warning,
        Message = "Underlying call for {MethodName} (identifier: {Identifier}, cache key: {CacheKey}) was canceled. Not caching canceled task.")]
    public static partial void LogUnderlyingCallCancelled(ILogger logger, string methodName, string identifier, string cacheKey);

    [LoggerMessage(
        EventId = EvtCanNotAccessResultOfCompletedTaskForCaching,
        Level = LogLevel.Error,
        Message = "Error accessing result of completed task for caching. Method: {MethodName}, Identifier: {Identifier}")]
    public static partial void LogCanNotAccessResultOfCompletedTaskForCaching(ILogger logger, string methodName, string identifier, Exception? exception);

    [LoggerMessage(
        EventId = EvtCanNotExtractResultForCaching,
        Level = LogLevel.Warning,
        Message = "Return type of {MethodName} is Task but not Task<T>. Cannot extract result for caching.")]
    public static partial void LogCanNotExtractResultForCaching(ILogger logger, string methodName);

    [LoggerMessage(
        EventId = EvtCachingTheResult,
        Level = LogLevel.Debug,
        Message = "Caching result for {MethodName} (identifier: {Identifier}) with key '{CacheKey}'. Result: {ResultIsNullOrNotNull}")]
    public static partial void LogCachingTheResult(ILogger logger, string methodName, string identifier, string cacheKey, string resultIsNullOrNotNull);

    [LoggerMessage(
        EventId = EvtFetchedButNotCachedBecauseExpiration,
        Level = LogLevel.Warning,
        Message = "Result for {MethodName} (identifier: {Identifier}) fetched but not cached as no valid expiration was configured.")]
    public static partial void LogFetchedButNotCachedBecauseExpiration(ILogger logger, string methodName, string identifier);

    [LoggerMessage(
        EventId = EvtInterceptedButNotReturnedTask,
        Level = LogLevel.Warning,
        Message = "Method {MethodName} was intercepted but did not return a Task. Caching for non-Task return types is not implemented in this interceptor.")]
    public static partial void LogInterceptedButNotReturnedTask(ILogger logger, string methodName);

    [LoggerMessage(
        EventId = EvtCriteriaOrArgumentMisconfiguration,
        Level = LogLevel.Trace,
        Message = "Method {MethodName} does not match caching criteria or arguments are invalid. Proceeding without caching.")]
    public static partial void LogCriteriaOrArgumentMisconfiguration(ILogger logger, string methodName);
    
}
