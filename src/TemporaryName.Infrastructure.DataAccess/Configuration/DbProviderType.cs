namespace TemporaryName.Infrastructure.DataAccess.Configuration;

/// <summary>
/// Specifies the supported database provider types.
/// This enum is used by the <see cref="IDbConnectionFactory"/> to determine
/// which ADO.NET provider to use for creating connections.
/// The string values (e.g., "PostgreSql") should match configuration entries.
/// </summary>
public enum DbProviderType
{
    /// <summary>
    /// Represents the PostgreSQL database system.
    /// Requires the Npgsql ADO.NET provider.
    /// </summary>
    PostgreSql = 0,

    /// <summary>
    /// Represents the Microsoft SQL Server database system.
    /// Requires the Microsoft.Data.SqlClient ADO.NET provider.
    /// </summary>
    SqlServer = 1,
    /// <summary>
    /// Represents the SQLite database system.
    /// Requires the sqlite's ADO.NET provider.
    /// </summary>
    SqLite = 2,
    /// <summary>
    /// Represents the MySQL database system.
    /// Requires the MySql's ADO.NET provider.
    /// </summary>
    MySql = 3,

}
