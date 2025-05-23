using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Caching.Redis.Implementations;

public partial class RedisCacheService 
{
    private const int ClassId = 10;
    private const int BaseEventId = Logging.CachingRedisBaseEventId + (ClassId * Logging.IncrementPerClass);

    private const int EvtCacheGetAsync = BaseEventId + (0 * Logging.IncrementPerLog);
    private const int EvtCacheGetHit = BaseEventId + (1 * Logging.IncrementPerLog);
    private const int EvtCacheGetMiss = BaseEventId + (2 * Logging.IncrementPerLog);
    private const int EvtCacheSetAsync = BaseEventId + (3 * Logging.IncrementPerLog);
    private const int EvtCacheRemoveAsync = BaseEventId + (4 * Logging.IncrementPerLog);
    private const int EvtCacheExistsAsync = BaseEventId + (5 * Logging.IncrementPerLog);
    private const int EvtCacheExistsHit = BaseEventId + (6 * Logging.IncrementPerLog);
    private const int EvtCacheExistsMiss = BaseEventId + (7 * Logging.IncrementPerLog);
    private const int EvtCacheGetOrSetFactoryExecuting = BaseEventId + (8 * Logging.IncrementPerLog);
    private const int EvtCacheGetOrSetFactoryCompleted = BaseEventId + (9 * Logging.IncrementPerLog);
    private const int EvtCacheSerializationError = BaseEventId + (10 * Logging.IncrementPerLog);
    private const int EvtCacheDeserializationError = BaseEventId + (11 * Logging.IncrementPerLog);
    private const int EvtCacheOperationError = BaseEventId + (12 * Logging.IncrementPerLog);
    private const int EvtCacheRemoveByPrefixAsync = BaseEventId + (13 * Logging.IncrementPerLog);
    private const int EvtCacheRemoveByPrefixScan = BaseEventId + (14 * Logging.IncrementPerLog);
    private const int EvtCacheRemoveByPrefixDelete = BaseEventId + (15 * Logging.IncrementPerLog);
    private const int EvtCacheProviderError = BaseEventId + (16 * Logging.IncrementPerLog);


    [LoggerMessage(EventId = EvtCacheGetAsync, Level = LogLevel.Debug, Message = "Cache GET: Key '{CacheKey}'.")]
    public static partial void LogCacheGetAsync(ILogger logger, string cacheKey);

    [LoggerMessage(EventId = EvtCacheGetHit, Level = LogLevel.Debug, Message = "Cache HIT: Key '{CacheKey}'. Type: '{CacheType}'.")]
    public static partial void LogCacheGetHit(ILogger logger, string cacheKey, string cacheType);

    [LoggerMessage(EventId = EvtCacheGetMiss, Level = LogLevel.Debug, Message = "Cache MISS: Key '{CacheKey}'.")]
    public static partial void LogCacheGetMiss(ILogger logger, string cacheKey);

    [LoggerMessage(EventId = EvtCacheSetAsync, Level = LogLevel.Debug, Message = "Cache SET: Key '{CacheKey}'. Type: '{CacheType}'. Options: AbsoluteExpiration='{AbsoluteExpiration}', SlidingExpiration='{SlidingExpiration}'.")]
    public static partial void LogCacheSetAsync(ILogger logger, string cacheKey, string cacheType, DateTimeOffset? absoluteExpiration, TimeSpan? slidingExpiration);

    [LoggerMessage(EventId = EvtCacheRemoveAsync, Level = LogLevel.Debug, Message = "Cache REMOVE: Key '{CacheKey}'.")]
    public static partial void LogCacheRemoveAsync(ILogger logger, string cacheKey);

    [LoggerMessage(EventId = EvtCacheExistsAsync, Level = LogLevel.Debug, Message = "Cache EXISTS: Key '{CacheKey}'.")]
    public static partial void LogCacheExistsAsync(ILogger logger, string cacheKey);

    [LoggerMessage(EventId = EvtCacheExistsHit, Level = LogLevel.Debug, Message = "Cache EXISTS HIT: Key '{CacheKey}' found.")]
    public static partial void LogCacheExistsHit(ILogger logger, string cacheKey);

    [LoggerMessage(EventId = EvtCacheExistsMiss, Level = LogLevel.Debug, Message = "Cache EXISTS MISS: Key '{CacheKey}' not found.")]
    public static partial void LogCacheExistsMiss(ILogger logger, string cacheKey);

    [LoggerMessage(EventId = EvtCacheGetOrSetFactoryExecuting, Level = LogLevel.Debug, Message = "Cache GET_OR_SET: Key '{CacheKey}' not found in cache. Executing factory.")]
    public static partial void LogCacheGetOrSetFactoryExecuting(ILogger logger, string cacheKey);

    [LoggerMessage(EventId = EvtCacheGetOrSetFactoryCompleted, Level = LogLevel.Debug, Message = "Cache GET_OR_SET: Key '{CacheKey}'. Factory executed. Result type: '{ResultType}'. Caching result.")]
    public static partial void LogCacheGetOrSetFactoryCompleted(ILogger logger, string cacheKey, string resultType);

    [LoggerMessage(EventId = EvtCacheSerializationError, Level = LogLevel.Error, Message = "Cache SERIALIZATION ERROR for Key '{CacheKey}': {ErrorMessage}")]
    public static partial void LogCacheSerializationError(ILogger logger, string cacheKey, string errorMessage, Exception exception);

    [LoggerMessage(EventId = EvtCacheDeserializationError, Level = LogLevel.Warning, Message = "Cache DESERIALIZATION ERROR for Key '{CacheKey}': {ErrorMessage}. Returning null/default.")]
    public static partial void LogCacheDeserializationError(ILogger logger, string cacheKey, string errorMessage, Exception exception);

    [LoggerMessage(EventId = EvtCacheOperationError, Level = LogLevel.Error, Message = "Cache Operation ERROR for Key '{CacheKey}', Operation: '{Operation}': {ErrorMessage}")]
    public static partial void LogCacheOperationError(ILogger logger, string cacheKey, string operation, string errorMessage, Exception exception);

    [LoggerMessage(EventId = EvtCacheRemoveByPrefixAsync, Level = LogLevel.Information, Message = "Cache REMOVE_BY_PREFIX: Prefix '{Prefix}'.")]
    public static partial void LogCacheRemoveByPrefixAsync(ILogger logger, string prefix);

    [LoggerMessage(EventId = EvtCacheRemoveByPrefixScan, Level = LogLevel.Debug, Message = "Cache REMOVE_BY_PREFIX: Scanned and found {KeyCount} keys for prefix '{Prefix}'.")]
    public static partial void LogCacheRemoveByPrefixScan(ILogger logger, int keyCount, string prefix);

    [LoggerMessage(EventId = EvtCacheRemoveByPrefixDelete, Level = LogLevel.Debug, Message = "Cache REMOVE_BY_PREFIX: Deleted {DeletedCount} keys for prefix '{Prefix}'.")]
    public static partial void LogCacheRemoveByPrefixDelete(ILogger logger, long deletedCount, string prefix);

    [LoggerMessage(EventId = EvtCacheProviderError, Level = LogLevel.Error, Message = "Cache Provider ERROR (StackExchange.Redis) for Key '{CacheKey}', Operation: '{Operation}': {ErrorMessage}")]
    public static partial void LogCacheProviderError(ILogger logger, string? cacheKey, string operation, string errorMessage, Exception ex);
}
