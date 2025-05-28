using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Olap.ClickHouseDb;

public partial class DependencyInjection
{

    private const int DiClassId = 12; 
    private const int DiBaseEventId = Logging.ClickHousePersistenceBaseEventId + (DiClassId * Logging.IncrementPerClass);
    public const int StartingRegistrationEventId = DiBaseEventId + (0 * Logging.IncrementPerLog);
    public const int OptionsConfiguredEventId = DiBaseEventId + (1 * Logging.IncrementPerLog);
    public const int ConnectionProviderRegisteredEventId = DiBaseEventId + (2 * Logging.IncrementPerLog);
    public const int RegistrationCompletedEventId = DiBaseEventId + (3 * Logging.IncrementPerLog);
 

    [LoggerMessage(
        EventId = StartingRegistrationEventId,
        Level = LogLevel.Information,
        Message = "ClickHouse DI: Starting ClickHouse persistence services registration.")]
    public static partial void LogStartingRegistration(ILogger logger);

    [LoggerMessage(
        EventId = OptionsConfiguredEventId,
        Level = LogLevel.Debug,
        Message = "ClickHouse DI: Configured options '{OptionsName}' from section '{ConfigurationSectionName}'.")]
    public static partial void LogOptionsConfigured(ILogger logger, string optionsName, string configurationSectionName);

    [LoggerMessage(
        EventId = ConnectionProviderRegisteredEventId,
        Level = LogLevel.Debug,
        Message = "ClickHouse DI: Registered connection provider '{InterfaceName}' with implementation '{ImplementationName}' (Lifetime: {ServiceLifetime}).")]
    public static partial void LogConnectionProviderRegistered(ILogger logger, string interfaceName, string implementationName, string serviceLifetime);

    [LoggerMessage(
        EventId = RegistrationCompletedEventId,
        Level = LogLevel.Information,
        Message = "ClickHouse DI: ClickHouse persistence services registration completed.")]
    public static partial void LogRegistrationCompleted(ILogger logger);
}
