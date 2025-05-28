using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j.Implementations;

public partial class Neo4jClientProvider
{
    private const int ClassId = 10;
    private const int BaseEventId = Logging.Neo4jPersistenceBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int AttemptingToCreateDriver = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int DriverCreatedSuccessfully = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int DriverCreationFailure = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int CreatingSession = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int DisposingDriver = BaseEventId + (4 * Logging.IncrementPerLog);
    public const int DriverDisposed = BaseEventId + (5 * Logging.IncrementPerLog);
    public const int EncryptionConfiguration = BaseEventId + (6 * Logging.IncrementPerLog);
    public const int TrustStrategy = BaseEventId + (7 * Logging.IncrementPerLog);
    public const int TrustStrategyInsecureWarning = BaseEventId + (8 * Logging.IncrementPerLog); // New
    public const int AuthenticationConfigured = BaseEventId + (9 * Logging.IncrementPerLog); // New
    public const int ConfigurationError = BaseEventId + (10 * Logging.IncrementPerLog); // New


    [LoggerMessage(EventId = AttemptingToCreateDriver, Level = LogLevel.Information, Message = "Neo4jClientProvider: Attempting to create Neo4j Driver for URI: {DriverUri}.")]
    public static partial void LogAttemptingToCreateDriver(ILogger logger, string driverUri);

    [LoggerMessage(EventId = DriverCreatedSuccessfully, Level = LogLevel.Information, Message = "Neo4jClientProvider: Neo4j Driver created successfully for URI: {DriverUri}.")]
    public static partial void LogDriverCreatedSuccessfully(ILogger logger, string driverUri);

    [LoggerMessage(EventId = DriverCreationFailure, Level = LogLevel.Critical, Message = "Neo4jClientProvider: Failed to create Neo4j Driver for URI: {DriverUri}. Error: {ErrorMessage}")]
    public static partial void LogDriverCreationFailure(ILogger logger, string driverUri, string errorMessage, Exception ex);

    [LoggerMessage(EventId = CreatingSession, Level = LogLevel.Debug, Message = "Neo4jClientProvider: Creating Neo4j session. AccessMode: {AccessMode}, Database: {DatabaseName}.")]
    public static partial void LogCreatingSession(ILogger logger, string accessMode, string databaseName);

    [LoggerMessage(EventId = DisposingDriver, Level = LogLevel.Information, Message = "Neo4jClientProvider: Disposing Neo4j Driver for URI: {DriverUri}.")]
    public static partial void LogDisposingDriver(ILogger logger, string driverUri);

    [LoggerMessage(EventId = DriverDisposed, Level = LogLevel.Information, Message = "Neo4jClientProvider: Neo4j Driver disposed for URI: {DriverUri}.")]
    public static partial void LogDriverDisposed(ILogger logger, string driverUri);

    [LoggerMessage(EventId = EncryptionConfiguration, Level = LogLevel.Debug, Message = "Neo4jClientProvider: Driver encryption is {EncryptionState}.")]
    public static partial void LogEncryptionConfiguration(ILogger logger, string encryptionState);
    [LoggerMessage(EventId = TrustStrategy, Level = LogLevel.Debug, Message = "Neo4jClientProvider: Trust strategy configured as: {Strategy}.")]
    public static partial void LogTrustStrategy(ILogger logger, string strategy);

    [LoggerMessage(EventId = TrustStrategyInsecureWarning, Level = LogLevel.Warning, Message = "Neo4jClientProvider: INSECURE Trust strategy configured: {Strategy}. THIS IS NOT SAFE FOR PRODUCTION.")]
    public static partial void LogTrustStrategyInsecureWarning(ILogger logger, string strategy);

    [LoggerMessage(EventId = AuthenticationConfigured, Level = LogLevel.Debug, Message = "Neo4jClientProvider: Authentication configured as: {AuthType}.")]
    public static partial void LogAuthenticationConfigured(ILogger logger, string authType);

    [LoggerMessage(EventId = ConfigurationError, Level = LogLevel.Error, Message = "Neo4jClientProvider Configuration Error: Code='{ErrorCode}', Description='{ErrorDescription}'.")]
    public static partial void LogConfigurationError(ILogger logger, string errorCode, string? errorDescription, Exception? ex);
}