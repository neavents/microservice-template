using System;
using SharedKernel.Primitives;

namespace SharedKernel.Exceptions.Infrastructure;

public class SecurityException : InfrastructureException
{
    public SecurityException(Error error) : base(error)
    {
    }

    public SecurityException(Error error, Exception innerException) : base(error, innerException)
    {
    }

    public SecurityException(string message, Error error) : base(message, error)
    {
    }

    public SecurityException(string message, Error error, Exception innerException) : base(message, error, innerException)
    {
    }
}
