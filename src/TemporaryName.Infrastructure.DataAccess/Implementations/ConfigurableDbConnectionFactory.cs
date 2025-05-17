using System;
using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.DataAccess.Abstractions;
using TemporaryName.Infrastructure.DataAccess.Configuration;
using TemporaryName.Infrastructure.DataAccess.Exceptions;

namespace TemporaryName.Infrastructure.DataAccess.Implementations;

/// <summary>
    /// A database connection factory that creates connections based on settings
    /// retrieved from <see cref="IConfiguration"/>. It supports multiple database providers.
    /// Expects configuration for connection strings and their associated providers, e.g.:
    /// "ConnectionStrings": {
    ///   "MyDatabase": "Server=...",
    ///   "MyDatabase:Provider": "PostgreSql"
    /// }
    /// </summary>
    public class ConfigurableDbConnectionFactory : IDbConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConfigurableDbConnectionFactory> _logger;

        public ConfigurableDbConnectionFactory(IConfiguration configuration, ILogger<ConfigurableDbConnectionFactory> logger)
        {
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _configuration = configuration;
            _logger = logger;
        }

        public async Task<DbConnection> CreateOpenConnectionAsync(string connectionStringName, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName, nameof(connectionStringName));

            string? connectionString = _configuration.GetConnectionString(connectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                string errorMessage = $"Connection string '{connectionStringName}' not found or is empty in configuration.";
                _logger.LogError(errorMessage);
                throw new ConnectionStringNotFoundException(errorMessage, connectionStringName);
            }

            string providerConfigKey = $"{ConfigurationPath.Combine("ConnectionStrings", connectionStringName)}:Provider";
            string? providerName = _configuration[providerConfigKey];

            if (string.IsNullOrWhiteSpace(providerName))
            {
                string errorMessage = $"Database provider configuration key '{providerConfigKey}' not found or is empty for connection string '{connectionStringName}'.";
                _logger.LogError(errorMessage);
                // You could default here, but explicit configuration is safer for production.
                throw new ConnectionStringNotFoundException(errorMessage, connectionStringName);
            }

            if (!Enum.TryParse<DbProviderType>(providerName, ignoreCase: true, out DbProviderType providerType))
            {
                string errorMessage = $"Unsupported database provider '{providerName}' configured for connection string '{connectionStringName}'.";
                _logger.LogError(errorMessage);
                throw new UnsupportedDbProviderException(errorMessage, providerName, connectionStringName);
            }

            _logger.LogDebug("Creating database connection for '{ConnectionStringName}' using provider '{ProviderType}'.", connectionStringName, providerType);

            DbConnection? connection = null;
            try
            {
                connection = providerType switch
                {
                    DbProviderType.PostgreSql => new NpgsqlConnection(connectionString),
                    DbProviderType.SqlServer => new SqlConnection(connectionString),
                    _ => throw new UnsupportedDbProviderException( // Should be caught by Enum.TryParse, but as a safeguard.
                            $"Database provider '{providerType}' is defined in enum but not handled in factory.",
                            providerType.ToString(),
                            connectionStringName)
                };

                await connection.OpenAsync(cancellationToken).ConfigureAwait(false); // ConfigureAwait(false) is good practice in library code.
                _logger.LogDebug("Database connection for '{ConnectionStringName}' opened successfully.", connectionStringName);
                return connection;
            }
            catch (DbException ex) // Catches NpgsqlException, SqlException, etc.
            {
                // Clean up connection if created but failed to open or on other DbException
                if (connection != null)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
                string errorMessage = $"Failed to open database connection for '{connectionStringName}' using provider '{providerType}'.";
                _logger.LogError(ex, errorMessage);
                throw new DbConnectionOpenException(errorMessage, ex, connectionStringName);
            }
            catch (Exception ex) // Catch other unexpected errors during instantiation or opening
            {
                if (connection != null)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
                string errorMessage = $"An unexpected error occurred while creating or opening database connection for '{connectionStringName}'.";
                _logger.LogError(ex, errorMessage);
                // Rethrow as a DbConnectionFactoryException or a more specific one if identifiable.
                // For now, a general DbConnectionFactoryException or rethrow if it's truly unexpected and not a DB issue.
                // DbConnectionOpenException is suitable if the error happened during the open/creation phase.
                throw new DbConnectionOpenException(errorMessage, ex, connectionStringName);
            }
        }
    }
