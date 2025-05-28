using System;

namespace TemporaryName.Infrastructure.DataAccess.Exceptions;

/// <summary>
/// Base exception for errors originating from the <see cref="Abstractions.IDbConnectionFactory"/> implementations.
/// </summary>
public class DbConnectionFactoryException : Exception
{
    public string? ConnectionStringNameAttempted { get; }

    public DbConnectionFactoryException(string message, string? connectionStringNameAttempted = null)
        : base(message)
    {
        ConnectionStringNameAttempted = connectionStringNameAttempted;
    }

    public DbConnectionFactoryException(string message, Exception innerException, string? connectionStringNameAttempted = null)
        : base(message, innerException)
    {
        ConnectionStringNameAttempted = connectionStringNameAttempted;
    }
}
