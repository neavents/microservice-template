using System;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Exceptions;

public class TransientException : Exception
{
    public TransientException(string message) : base(message) { }
    public TransientException(string message, Exception inner) : base(message, inner) { }
}
