using System;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Exceptions;

public class NonTransientBusinessException : Exception
{
    public NonTransientBusinessException(string message) : base(message) { }
    public NonTransientBusinessException(string message, Exception inner) : base(message, inner) { }
}
