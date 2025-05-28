using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Olap.ClickHouseDb.Implementations;

public partial class ClickHouseConnectionProvider
{
    private const int ClassId = 10;
    private const int BaseEventId = Logging.ClickHousePersistenceBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int MissingConnectionString = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int ConnectionStringConfigured = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int CreatingConnection = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int OpeningConnection = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int ConnectionOpenedSuccessfully = BaseEventId + (4 * Logging.IncrementPerLog);
    public const int ConnectionOpenFailure = BaseEventId + (5 * Logging.IncrementPerLog);
    public const int DisposingProvider = BaseEventId + (6 * Logging.IncrementPerLog);
    public const int SslOptionMismatchWarning = BaseEventId + (7 * Logging.IncrementPerLog); 
    

    [LoggerMessage(EventId = MissingConnectionString, Level = LogLevel.Error, Message = "ClickHouseConnectionProvider: ClickHouse connection string is missing or empty in configuration.")]
    public static partial void LogMissingConnectionString(ILogger logger);

    [LoggerMessage(EventId = ConnectionStringConfigured, Level = LogLevel.Debug, Message = "ClickHouseConnectionProvider: Connection string configured: {ConnectionString} (password redacted). UseSsl option: {UseSslFlag}.")] // Updated
    public static partial void LogConnectionStringConfigured(ILogger logger, string connectionString, bool useSslFlag);

    [LoggerMessage(EventId = CreatingConnection, Level = LogLevel.Trace, Message = "ClickHouseConnectionProvider: Creating new ClickHouse DbConnection instance using ClickHouse.Client.")]
    public static partial void LogCreatingConnection(ILogger logger);

    [LoggerMessage(EventId = OpeningConnection, Level = LogLevel.Trace, Message = "ClickHouseConnectionProvider: Attempting to open ClickHouse connection.")]
    public static partial void LogOpeningConnection(ILogger logger);

    [LoggerMessage(EventId = ConnectionOpenedSuccessfully, Level = LogLevel.Debug, Message = "ClickHouseConnectionProvider: ClickHouse connection opened successfully.")]
    public static partial void LogConnectionOpenedSuccessfully(ILogger logger);

    [LoggerMessage(EventId = ConnectionOpenFailure, Level = LogLevel.Error, Message = "ClickHouseConnectionProvider: Failed to open ClickHouse connection. Error: {ErrorMessage}")]
    public static partial void LogConnectionOpenFailure(ILogger logger, string errorMessage, Exception ex);

    [LoggerMessage(EventId = DisposingProvider, Level = LogLevel.Debug, Message = "ClickHouseConnectionProvider: Disposing.")]
    public static partial void LogDisposingProvider(ILogger logger);

    [LoggerMessage(EventId = SslOptionMismatchWarning, Level = LogLevel.Warning, Message = "ClickHouseConnectionProvider: UseSsl option mismatch with connection string content. ConnectionString: '{ConnectionString}'. Details: {Details}. Ensure SSL settings in ConnectionString are correct for desired security.")] // New
    public static partial void LogSslOptionMismatchWarning(ILogger logger, string connectionString, string details = "UseSsl is true, but connection string does not explicitly enable SSL, or vice-versa.");
}
