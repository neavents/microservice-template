using System;

namespace TemporaryName.Infrastructure.DataAccess.Exceptions;

/// <summary>
/// Exception thrown when an error occurs while trying to open a database connection.
/// </summary>
public class DbConnectionOpenException : DbConnectionFactoryException
{
    public DbConnectionOpenException(string message, Exception innerException, string? connectionStringNameAttempted = null)
        : base(message, innerException, connectionStringNameAttempted)
    {
    }
}