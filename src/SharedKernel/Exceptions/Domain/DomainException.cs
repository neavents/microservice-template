using System;
using SharedKernel.Primitives;

namespace SharedKernel.Exceptions.Domain;

public class DomainException : Exception
{
    /// <summary>
    /// Gets the structured error details associated with this exception.
    /// </summary>
    public Error ErrorDetails { get; }

    protected DomainException(Error error) : base(error.Description ?? error.Code)
    {
        ErrorDetails = error;
    }

    protected DomainException(Error error, Exception innerException) : base(error.Description ?? error.Code, innerException)
    {
        ErrorDetails = error;
    }

    protected DomainException(string message, Error error) : base(message)
    {
        ErrorDetails = error;
    }

    protected DomainException(string message, Error error, Exception innerException) : base(message, innerException)
    {
        ErrorDetails = error;
    }
}
