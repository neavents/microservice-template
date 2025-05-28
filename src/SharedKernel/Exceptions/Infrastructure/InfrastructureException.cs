using System;
using SharedKernel.Primitives;

namespace SharedKernel.Exceptions.Infrastructure;

public class InfrastructureException : Exception
{
    /// <summary>
    /// Gets the structured error details associated with this exception.
    /// </summary>
    public Error ErrorDetails { get; }

    protected InfrastructureException(Error error) : base(error.Description ?? error.Code)
    {
        ErrorDetails = error;
    }

    protected InfrastructureException(Error error, Exception innerException) : base(error.Description ?? error.Code, innerException)
    {
        ErrorDetails = error;
    }

    protected InfrastructureException(string message, Error error) : base(message)
    {
        ErrorDetails = error;
    }

    protected InfrastructureException(string message, Error error, Exception innerException) : base(message, innerException)
    {
        ErrorDetails = error;
    }
}
