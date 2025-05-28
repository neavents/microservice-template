using System;

namespace TemporaryName.Infrastructure.DataAccess.Exceptions;

/// <summary>
/// Exception thrown when the configured database provider is not supported.
/// </summary>
public class UnsupportedDbProviderException : DbConnectionFactoryException
{
    public string? ProviderAttempted { get; }

    public UnsupportedDbProviderException(string message, string? providerAttempted, string? connectionStringNameAttempted = null)
        : base(message, connectionStringNameAttempted)
    {
        ProviderAttempted = providerAttempted;
    }
}
