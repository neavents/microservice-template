using System;
using SharedKernel.Primitives;

namespace SharedKernel.Exceptions.Application;

public class ApplicationException : Exception
{
    /// <summary>
    /// Gets the structured error details associated with this exception.
    /// </summary>
    public Error ErrorDetails { get; }

    protected ApplicationException(Error error) : base(error.Description ?? error.Code)
    {
        ErrorDetails = error;
    }

    protected ApplicationException(Error error, Exception innerException) : base(error.Description ?? error.Code, innerException)
    {
        ErrorDetails = error;
    }

    protected ApplicationException(string message, Error error) : base(message)
    {
        ErrorDetails = error;
    }

    protected ApplicationException(string message, Error error, Exception innerException) : base(message, innerException)
    {
        ErrorDetails = error;
    }
}
