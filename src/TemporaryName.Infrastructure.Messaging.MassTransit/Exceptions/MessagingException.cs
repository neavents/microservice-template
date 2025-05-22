using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Exceptions;

public class MessagingException : Exception
{
    public Error Error { get; }

    public MessagingException(Error error) : base(error.Description)
    {
        Error = error;
    }

    public MessagingException(Error error, Exception innerException) : base(error.Description, innerException)
    {
        Error = error;
    }

    public MessagingException(string message, Error error) : base(message)
    {
        Error = error;
    }

    public MessagingException(string message, Error error, Exception innerException) : base(message, innerException)
    {
        Error = error;
    }
}
