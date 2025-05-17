using System;
using System.Data.Common;

namespace TemporaryName.Infrastructure.DataAccess.Abstractions;

/// <summary>
/// Defines a contract for a factory that creates and opens database connections.
/// This abstraction allows different database providers and connection strategies to be used interchangeably by consumers like repositories or stores.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Asynchronously creates, opens, and returns a database connection based on the specified connection string name.
    /// The factory is responsible for determining the appropriate database provider and connection details
    /// from application configuration.
    /// </summary>
    /// <param name="connectionStringName">The logical name of the connection string as defined in the application's configuration.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an open <see cref="DbConnection"/>.
    /// </returns>
    /// <exception cref="TemporaryName.Infrastructure.DataAccess.Exceptions.ConnectionStringNotFoundException">
    /// Thrown if the connection string or its associated provider configuration is not found for the given name.
    /// </exception>
    /// <exception cref="TemporaryName.Infrastructure.DataAccess.Exceptions.UnsupportedDbProviderException">
    /// Thrown if the configured database provider is not supported by this factory.
    /// </exception>
    /// <exception cref="TemporaryName.Infrastructure.DataAccess.Exceptions.DbConnectionOpenException">
    /// Thrown if an error occurs while attempting to open the database connection.
    /// This typically wraps a <see cref="DbException"/> from the underlying provider.
    /// </exception>
    Task<DbConnection> CreateOpenConnectionAsync(string connectionStringName, CancellationToken cancellationToken = default);
}
