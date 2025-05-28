using System;
using SharedKernel.Primitives;

namespace SharedKernel.Exceptions.Infrastructure;

public class CachingException : InfrastructureException
{
    protected CachingException(Error error) : base(error)
    {
    }

    protected CachingException(Error error, Exception innerException) : base(error, innerException)
    {
    }

    protected CachingException(string message, Error error) : base(message, error)
    {
    }

    protected CachingException(string message, Error error, Exception innerException) : base(message, error, innerException)
    {
    }
}
