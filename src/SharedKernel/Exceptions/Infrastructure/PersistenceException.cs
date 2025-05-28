using System;
using SharedKernel.Primitives;

namespace SharedKernel.Exceptions.Infrastructure;

public class PersistenceException : InfrastructureException
{
    public string PersistenceProviderName { get; } = string.Empty;
    public PersistenceException(Error error, string providerName) : base(error)
    {
        PersistenceProviderName = providerName;
    }

    public PersistenceException(Error error, Exception innerException, string providerName) : base(error, innerException)
    {
        PersistenceProviderName = providerName;
    }

    public PersistenceException(string message, Error error, string providerName) : base(message, error)
    {
        PersistenceProviderName = providerName;
    }

    public PersistenceException(string message, Error error, Exception innerException, string providerName) : base(message, error, innerException)
    {
        PersistenceProviderName = providerName;
    }
}
