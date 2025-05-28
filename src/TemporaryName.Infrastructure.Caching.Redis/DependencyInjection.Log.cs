using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Caching.Redis;


public static partial class DependencyInjection
{
    private const int ClassId = 1;
    private const int BaseEventId = Logging.CachingRedisBaseEventId + (ClassId * Logging.IncrementPerClass);

    private const int EvtStartingRegistration = BaseEventId + (0 * Logging.IncrementPerLog);
    private const int EvtOptionsConfigured = BaseEventId + (1 * Logging.IncrementPerLog);
    private const int EvtRedisUntrustedSSLAllowed = EvtOptionsConfigured + 1;
    private const int EvtRedisConnectionMultiplexerRegistered = BaseEventId + (2 * Logging.IncrementPerLog);
    private const int EvtRedisConnectionFailed = BaseEventId + (3 * Logging.IncrementPerLog);
    private const int EvtCacheServiceRegistered = BaseEventId + (4 * Logging.IncrementPerLog);
    private const int EvtRegistrationCompleted = BaseEventId + (5 * Logging.IncrementPerLog);
    private const int EvtCacheKeyServiceRegistered = BaseEventId + (6 * Logging.IncrementPerLog);


    [LoggerMessage(EventId = EvtStartingRegistration, Level = LogLevel.Information, Message = "Starting {ProjectName} services registration.")]
    public static partial void LogStartingRegistration(ILogger logger, string projectName = Logging.ProjectName);

    [LoggerMessage(EventId = EvtOptionsConfigured, Level = LogLevel.Information, Message = "{OptionsName} configured from section '{ConfigSection}'.")]
    public static partial void LogOptionsConfigured(ILogger logger, string optionsName, string configSection);
    [LoggerMessage(
        EventId = EvtRedisUntrustedSSLAllowed,
        Level = LogLevel.Critical,
        Message = "Redis connection allows untrusted SSL certificates. This is insecure and should only be used for development/testing."
    )]
    public static partial void LogRedisUntrustedSSLAllowed(ILogger logger);

    [LoggerMessage(EventId = EvtRedisConnectionMultiplexerRegistered, Level = LogLevel.Information, Message = "IConnectionMultiplexer registered as Singleton. ConnectionString: '{ConnectionString}'.")]
    public static partial void LogRedisConnectionMultiplexerRegistered(ILogger logger, string connectionString);
    
    [LoggerMessage(EventId = EvtRedisConnectionFailed, Level = LogLevel.Critical, Message = "Failed to connect to Redis: {ErrorMessage}. Caching will not be available.")]
    public static partial void LogRedisConnectionFailed(ILogger logger, string errorMessage, Exception ex);

    [LoggerMessage(EventId = EvtCacheServiceRegistered, Level = LogLevel.Information, Message = "{InterfaceName} registered as {ImplementationName} ({Lifetime}).")]
    public static partial void LogCacheServiceRegistered(ILogger logger, string interfaceName, string implementationName, string lifetime);
    
    [LoggerMessage(EventId = EvtCacheKeyServiceRegistered, Level = LogLevel.Information, Message = "{InterfaceName} registered as {ImplementationName} ({Lifetime}).")]
    public static partial void LogCacheKeyServiceRegistered(ILogger logger, string interfaceName, string implementationName, string lifetime);

    [LoggerMessage(EventId = EvtRegistrationCompleted, Level = LogLevel.Information, Message = "{ProjectName} services registration completed.")]
    public static partial void LogRegistrationCompleted(ILogger logger, string projectName = Logging.ProjectName);
}
