using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j;

public static partial class DependencyInjection
{
    private const int ClassId = 1;
    private const int BaseEventId = Logging.Neo4jPersistenceBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int StartingRegistration = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int OptionsConfigured = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int ClientProviderRegistered = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int RegistrationCompleted = BaseEventId + (3 * Logging.IncrementPerLog);

    [LoggerMessage(EventId = StartingRegistration, Level = LogLevel.Information, Message = "{ProjectName}: Starting Neo4j persistence services registration.")]
    public static partial void LogStartingRegistration(ILogger logger, string projectName = Logging.ProjectName);

    [LoggerMessage(EventId = OptionsConfigured, Level = LogLevel.Information, Message = "{ProjectName}: {OptionsName} configured from section '{ConfigSectionName}'. Validation on start is enabled.")]
    public static partial void LogOptionsConfigured(ILogger logger, string optionsName, string configSectionName, string projectName = Logging.ProjectName);

    [LoggerMessage(EventId = ClientProviderRegistered, Level = LogLevel.Information, Message = "{ProjectName}: {InterfaceName} registered as {ImplementationName} ({Lifetime}).")]
    public static partial void LogClientProviderRegistered(ILogger logger, string interfaceName, string implementationName, string lifetime, string projectName = Logging.ProjectName);

    [LoggerMessage(EventId = RegistrationCompleted, Level = LogLevel.Information, Message = "{ProjectName}: Neo4j persistence services registration completed.")]
    public static partial void LogRegistrationCompleted(ILogger logger, string projectName = Logging.ProjectName);
}
