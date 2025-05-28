using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.MultiTenancy;

public partial class DependencyInjection
{
    private const int ClassId = 170;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int EvtStartingRegistration = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtConfiguredFrom = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtConfiguredAndRegistered = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int EvtRegisteredAs = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int EvtEssentialServicesEnsured = BaseEventId + (4 * Logging.IncrementPerLog);
    public const int EvtResolvingViaFactoryMethod = BaseEventId + (5 * Logging.IncrementPerLog);
    public const int EvtResolvedToType = BaseEventId + (6 * Logging.IncrementPerLog);
    public const int EvtRegisteredIn = BaseEventId + (7 * Logging.IncrementPerLog);
    public const int EvtBaseRegisteredViaFactory = BaseEventId + (8 * Logging.IncrementPerLog);
    public const int EvtRegistrationCompleted = BaseEventId + (9 * Logging.IncrementPerLog);

    [LoggerMessage(
        EventId = EvtStartingRegistration,
        Level = LogLevel.Information,
        Message = "Starting MultiTenancy core services registration.")]
    public static partial void LogStartingRegistration(ILogger logger);

    [LoggerMessage(
        EventId = EvtConfiguredFrom,
        Level = LogLevel.Information,
        Message = "{optionsName} configured from section '{ConfigSection}'.")]
    public static partial void LogConfiguredFrom(ILogger logger, string optionsName, string configSection);

    [LoggerMessage(
        EventId = EvtConfiguredAndRegistered,
        Level = LogLevel.Information,
        Message = "{OptionsName} configured and registered {MethodName}.")]
    public static partial void LogConfiguredAndRegistered(ILogger logger, string optionsName, string methodName = "directly");

    [LoggerMessage(
        EventId = EvtRegisteredAs,
        Level = LogLevel.Information,
        Message = "{InterfaceName} registered as {ClassName} ({Lifetime})."
    )]
    public static partial void LogRegisteredAs(ILogger logger, string interfaceName, string className, string lifetime);

    [LoggerMessage(
        EventId = EvtEssentialServicesEnsured,
        Level = LogLevel.Information,
        Message = "Essential services ({Services}) ensured."
    )]
    public static partial void LogEssentialServicesEnsured(ILogger logger, string services);

    [LoggerMessage(
        EventId = EvtResolvingViaFactoryMethod,
        Level = LogLevel.Debug,
        Message = "Resolving {ToBeResolvedType} via factory method using {ResolverType}. Store Type from options: {StoreType}"
    )]
    public static partial void LogResolvingViaFactoryMethod(ILogger logger, string toBeResolvedType, string resolverType, string storeType);

    [LoggerMessage(
        EventId = EvtResolvedToType,
        Level = LogLevel.Information,
        Message = "{ResolvedType} resolved to type {ActualStoreType} (based on configuration {ConfiguredStoreType})."
    )]
    public static partial void LogResolvedToType(ILogger logger, string resolvedType, string ActualStoreType, string ConfiguredStoreType);

    [LoggerMessage(
        EventId = EvtRegisteredIn,
        Level = LogLevel.Information,
        Message = "{RegisteredType} registered in the ASP.NET Core request pipeline."
    )]
    public static partial void LogRegisteredIn(ILogger logger, string registeredType);

    [LoggerMessage(
        EventId = EvtBaseRegisteredViaFactory,
        Level = LogLevel.Debug,
        Message = "Base {ClassName} registered ({Lifetime}) via factory method."
    )]
    public static partial void LogBaseRegisteredViaFactory(ILogger logger, string className, string lifetime);

    [LoggerMessage(
        EventId = EvtRegistrationCompleted,
        Level = LogLevel.Information,
        Message = "{ProjectName} core services registration completed."
    )]
    public static partial void LogRegistrationCompleted(ILogger logger, string projectName);

}
