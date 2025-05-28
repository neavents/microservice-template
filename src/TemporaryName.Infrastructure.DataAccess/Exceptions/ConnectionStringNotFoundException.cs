using System;

namespace TemporaryName.Infrastructure.DataAccess.Exceptions;

/// <summary>
/// Exception thrown when a connection string or its required provider configuration cannot be found.
/// </summary>
public class ConnectionStringNotFoundException : DbConnectionFactoryException
{
    public ConnectionStringNotFoundException(string message, string? connectionStringNameAttempted = null)
        : base(message, connectionStringNameAttempted)
    {
    }
}
