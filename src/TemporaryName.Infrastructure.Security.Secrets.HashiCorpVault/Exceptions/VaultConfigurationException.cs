using System;
using SharedKernel.Exceptions.Infrastructure;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Exceptions;

public class VaultConfigurationException : SecurityException
{
    public VaultConfigurationException(Error error) : base(error)
    {
    }
    public VaultConfigurationException(Error error, Exception innerException) : base(error, innerException)
    {
    }

    public VaultConfigurationException(string message, Error error) : base(message, error)
    {
    }

    public VaultConfigurationException(string message, Error error, Exception innerException) : base(message, error, innerException)
    {
    }
}
