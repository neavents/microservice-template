using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra.Implementations;

public partial class CassandraSessionProvider
{
    private const int ClassId = 10;
    private const int BaseEventId = Logging.CassandraPersistenceBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int AttemptingToCreateCluster = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int ClusterCreatedSuccessfully = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int ClusterCreationFailure = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int CredentialsConfigured = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int LoadBalancingPolicyConfigured = BaseEventId + (4 * Logging.IncrementPerLog);
    public const int SslConfiguration = BaseEventId + (5 * Logging.IncrementPerLog);
    public const int CreatingSession = BaseEventId + (6 * Logging.IncrementPerLog);
    public const int DisposingCluster = BaseEventId + (7 * Logging.IncrementPerLog);
    public const int ClusterDisposed = BaseEventId + (8 * Logging.IncrementPerLog);


    [LoggerMessage(EventId = AttemptingToCreateCluster, Level = LogLevel.Information, Message = "CassandraSessionProvider: Attempting to create Cassandra Cluster for ContactPoints: {ContactPoints}.")]
    public static partial void LogAttemptingToCreateCluster(ILogger logger, string contactPoints);

    [LoggerMessage(EventId = ClusterCreatedSuccessfully, Level = LogLevel.Information, Message = "CassandraSessionProvider: Cassandra Cluster created successfully for ContactPoints: {ContactPoints}.")]
    public static partial void LogClusterCreatedSuccessfully(ILogger logger, string contactPoints);

    [LoggerMessage(EventId = ClusterCreationFailure, Level = LogLevel.Critical, Message = "CassandraSessionProvider: Failed to create Cassandra Cluster for ContactPoints: {ContactPoints}. Error: {ErrorMessage}")]
    public static partial void LogClusterCreationFailure(ILogger logger, string contactPoints, string errorMessage, Exception ex);

    [LoggerMessage(EventId = CredentialsConfigured, Level = LogLevel.Debug, Message = "CassandraSessionProvider: Credentials configured for username: {Username}.")]
    public static partial void LogCredentialsConfigured(ILogger logger, string? username);

    [LoggerMessage(EventId = LoadBalancingPolicyConfigured, Level = LogLevel.Debug, Message = "CassandraSessionProvider: DCAwareRoundRobinPolicy configured for Local DC: {LocalDc}.")]
    public static partial void LogLoadBalancingPolicyConfigured(ILogger logger, string localDc);

    [LoggerMessage(EventId = SslConfiguration, Level = LogLevel.Debug, Message = "CassandraSessionProvider: SSL is {SslState}.")]
    public static partial void LogSslConfiguration(ILogger logger, string sslState);

    [LoggerMessage(EventId = CreatingSession, Level = LogLevel.Debug, Message = "CassandraSessionProvider: Getting/Creating Cassandra session for Keyspace: {KeyspaceName}.")]
    public static partial void LogCreatingSession(ILogger logger, string keyspaceName);

    [LoggerMessage(EventId = DisposingCluster, Level = LogLevel.Information, Message = "CassandraSessionProvider: Disposing Cassandra Cluster for ContactPoints: {ContactPoints}.")]
    public static partial void LogDisposingCluster(ILogger logger, string contactPoints);

    [LoggerMessage(EventId = ClusterDisposed, Level = LogLevel.Information, Message = "CassandraSessionProvider: Cassandra Cluster disposed for ContactPoints: {ContactPoints}.")]
    public static partial void LogClusterDisposed(ILogger logger, string contactPoints);
}
