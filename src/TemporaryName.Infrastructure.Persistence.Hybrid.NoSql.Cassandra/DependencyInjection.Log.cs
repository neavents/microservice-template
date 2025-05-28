using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra;

public static partial class DependencyInjection
{
    private const int ClassId = 1;
    private const int BaseEventId = Logging.CassandraPersistenceBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int StartingRegistration = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int OptionsConfigured = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int SessionProviderRegistered = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int RegistrationCompleted = BaseEventId + (3 * Logging.IncrementPerLog);

    [LoggerMessage(EventId = StartingRegistration, Level = LogLevel.Information, Message = "{ProjectName}: Starting Cassandra persistence services registration.")]
    public static partial void LogStartingRegistration(ILogger logger, string projectName = Logging.ProjectName);

    [LoggerMessage(EventId = OptionsConfigured, Level = LogLevel.Information, Message = "{ProjectName}: {OptionsName} configured from section '{ConfigSectionName}'. Validation on start is enabled.")]
    public static partial void LogOptionsConfigured(ILogger logger, string optionsName, string configSectionName, string projectName = Logging.ProjectName);

    [LoggerMessage(EventId = SessionProviderRegistered, Level = LogLevel.Information, Message = "{ProjectName}: {InterfaceName} registered as {ImplementationName} ({Lifetime}).")]
    public static partial void LogSessionProviderRegistered(ILogger logger, string interfaceName, string implementationName, string lifetime, string projectName = Logging.ProjectName);

    [LoggerMessage(EventId = RegistrationCompleted, Level = LogLevel.Information, Message = "{ProjectName}: Cassandra persistence services registration completed.")]
    public static partial void LogRegistrationCompleted(ILogger logger, string projectName = Logging.ProjectName);
}
