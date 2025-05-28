using System;
using SharedKernel.Exceptions.Infrastructure;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Exceptions;

public class VaultAccessException : SecurityException
{
    public VaultAccessException(Error error) : base(error)
    {
    }
    public VaultAccessException(Error error, Exception innerException) : base(error, innerException)
    {
    }

    public VaultAccessException(string message, Error error) : base(message, error)
    {
    }

    public VaultAccessException(string message, Error error, Exception innerException) : base(message, error, innerException)
    {
    }
}
