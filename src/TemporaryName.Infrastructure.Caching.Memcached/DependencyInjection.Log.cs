using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Caching.Memcached;

public static partial class DependencyInjection
{
    private const int ClassId = 1; 
    private const int BaseEventId = Logging.CachingMemcachedBaseEventId + (ClassId * Logging.IncrementPerClass);
    private const int EvtStartingRegistration = BaseEventId + (0 * Logging.IncrementPerLog);
    private const int EvtOptionsConfigured = BaseEventId + (1 * Logging.IncrementPerLog);
    private const int EvtMemcachedClientRegistered = BaseEventId + (2 * Logging.IncrementPerLog);
    private const int EvtMemcachedClientConfigError = BaseEventId + (3 * Logging.IncrementPerLog);
    private const int EvtCacheServiceRegistered = BaseEventId + (4 * Logging.IncrementPerLog);
    private const int EvtCacheKeyServiceRegistered = BaseEventId + (5 * Logging.IncrementPerLog);
    private const int EvtRegistrationCompleted = BaseEventId + (6 * Logging.IncrementPerLog);


    [LoggerMessage(EventId = EvtStartingRegistration, Level = LogLevel.Information, Message = "Starting {ProjectName} services registration.")]
    public static partial void LogStartingRegistration(ILogger logger, string projectName = Logging.ProjectName);

    [LoggerMessage(EventId = EvtOptionsConfigured, Level = LogLevel.Information, Message = "{OptionsName} configured from section '{ConfigSection}'. Servers: {ServerCount}")]
    public static partial void LogOptionsConfigured(ILogger logger, string optionsName, string configSection, int serverCount);
    
    [LoggerMessage(EventId = EvtMemcachedClientRegistered, Level = LogLevel.Information, Message = "IMemcachedClient registered. InstanceName: '{InstanceName}'.")]
    public static partial void LogMemcachedClientRegistered(ILogger logger, string? instanceName);
    
    [LoggerMessage(EventId = EvtMemcachedClientConfigError, Level = LogLevel.Error, Message = "Error configuring IMemcachedClient: {ErrorMessage}")]
    public static partial void LogMemcachedClientConfigError(ILogger logger, string errorMessage, Exception? ex = null);

    [LoggerMessage(EventId = EvtCacheServiceRegistered, Level = LogLevel.Information, Message = "{InterfaceName} (Key: {ServiceKey}) registered as {ImplementationName} ({Lifetime}).")]
    public static partial void LogCacheServiceRegistered(ILogger logger, string interfaceName, string serviceKey, string implementationName, string lifetime);

    [LoggerMessage(EventId = EvtCacheKeyServiceRegistered, Level = LogLevel.Information, Message = "{InterfaceName} registered as {ImplementationName} ({Lifetime}) for Memcached.")]
    public static partial void LogCacheKeyServiceRegistered(ILogger logger, string interfaceName, string implementationName, string lifetime);

    [LoggerMessage(EventId = EvtRegistrationCompleted, Level = LogLevel.Information, Message = "{ProjectName} services registration completed.")]
    public static partial void LogRegistrationCompleted(ILogger logger, string projectName = Logging.ProjectName);
}
