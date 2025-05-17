using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

public abstract class MultiTenancyException : Exception
{
    /// <summary>
    /// Gets the structured error details associated with this exception.
    /// </summary>
    public Error ErrorDetails { get; }

    protected MultiTenancyException(Error error) : base(error.Description ?? error.Code)
    {
        ErrorDetails = error;
    }

    protected MultiTenancyException(Error error, Exception innerException) : base(error.Description ?? error.Code, innerException)
    {
        ErrorDetails = error;
    }

    protected MultiTenancyException(string message, Error error) : base(message)
    {
        ErrorDetails = error;
    }

    protected MultiTenancyException(string message, Error error, Exception innerException) : base(message, innerException)
    {
        ErrorDetails = error;
    }
}
