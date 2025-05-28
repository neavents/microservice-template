using System;
using SharedKernel.Exceptions.Infrastructure;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Exceptions;

public class SecretNotFoundException : SecurityException
{
    public string SecretPath { get; }
    public SecretNotFoundException(string secretPath, Error error) : base(error)
    {
        SecretPath = secretPath;
    }
    public SecretNotFoundException(string secretPath, Error error, Exception innerException) : base(error, innerException)
    {
        SecretPath = secretPath;
    }

    public SecretNotFoundException(string secretPath, string message, Error error) : base(message, error)
    {
        SecretPath = secretPath;
    }

    public SecretNotFoundException(string secretPath, string message, Error error, Exception innerException) : base(message, error, innerException)
    {
        SecretPath = secretPath;
    }
}
