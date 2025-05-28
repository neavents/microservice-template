using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault;

public static partial class DependencyInjection
{
    private const int ClassId = 1;
    private const int BaseEventId = Logging.HashiCorpVaultBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int StartingRegistration = BaseEventId + 0;
    public const int OptionsConfigured = BaseEventId + 1;
    public const int ProviderRegistered = BaseEventId + 2;
    public const int ManagerRegistered = BaseEventId + 3;
    public const int RegistrationCompleted = BaseEventId + 4;


    [LoggerMessage(EventId = StartingRegistration, Level = LogLevel.Information, Message = "{ProjectName}: Starting HashiCorp Vault services registration.")]
    public static partial void LogStartingRegistration(ILogger logger, string projectName = Logging.ProjectName);

    [LoggerMessage(EventId = OptionsConfigured, Level = LogLevel.Information, Message = "{ProjectName}: {OptionsName} configured from section '{ConfigSectionName}'. Validation on start is enabled.")]
    public static partial void LogOptionsConfigured(ILogger logger, string optionsName, string configSectionName, string projectName = Logging.ProjectName);

    [LoggerMessage(EventId = ProviderRegistered, Level = LogLevel.Information, Message = "{ProjectName}: {InterfaceName} registered as {ImplementationName} ({Lifetime}).")]
    public static partial void LogProviderRegistered(ILogger logger, string interfaceName, string implementationName, string lifetime, string projectName = Logging.ProjectName);

    [LoggerMessage(EventId = ManagerRegistered, Level = LogLevel.Information, Message = "{ProjectName}: {InterfaceName} registered as {ImplementationName} ({Lifetime}).")]
    public static partial void LogManagerRegistered(ILogger logger, string interfaceName, string implementationName, string lifetime, string projectName = Logging.ProjectName);

    [LoggerMessage(EventId = RegistrationCompleted, Level = LogLevel.Information, Message = "{ProjectName}: HashiCorp Vault services registration completed.")]
    public static partial void LogRegistrationCompleted(ILogger logger, string projectName = Logging.ProjectName);
}
