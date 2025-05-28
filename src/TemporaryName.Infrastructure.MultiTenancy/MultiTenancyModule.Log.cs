using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.MultiTenancy;

public partial class MultiTenancyModule
{
    private const int ClassId = 180;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int EvtModuleState = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtRegistered = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtCachingState = BaseEventId + (2 * Logging.IncrementPerLog);

    [LoggerMessage(
        EventId = EvtModuleState,
        Level = LogLevel.Information,
        Message = "[Autofac Module] {ClassName} {State}. {Extra}"
    )]
    public static partial void LogModuleState(ILogger logger, string className, string state, string extra);

    [LoggerMessage(
        EventId = EvtRegistered,
        Level = LogLevel.Debug,
        Message = "[Autofac Module] {DependencyName} registered."
    )]
    public static partial void LogRegistered(ILogger logger, string dependencyName);

    [LoggerMessage(
        EventId = EvtCachingState,
        Level = LogLevel.Debug,
        Message = "[Autofac Module] Tenant store caching is {State}. {Extra}"
    )]
    public static partial void LogCachingState(ILogger logger, string state, string extra);
}
