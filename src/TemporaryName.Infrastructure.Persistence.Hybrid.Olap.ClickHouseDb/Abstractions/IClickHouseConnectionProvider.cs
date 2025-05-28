using System;
using System.Data.Common;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Olap.ClickHouseDb.Abstractions;

public interface IClickHouseConnectionProvider : IAsyncDisposable
{
    /// <summary>
    /// Creates a new ClickHouse database connection.
    /// The connection is not opened by this method.
    /// </summary>
    /// <returns>A <see cref="DbConnection"/> instance for ClickHouse.</returns>
    DbConnection CreateConnection();

    /// <summary>
    /// Asynchronously creates and opens a new ClickHouse database connection.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an open <see cref="DbConnection"/>.
    /// </returns>
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
