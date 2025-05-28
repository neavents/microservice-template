using System;
using System.Runtime.Serialization;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Exceptions;

public class MessagingConfigurationException : MessagingException
{
    public MessagingConfigurationException(Error error) : base(error)
    {
    }

    public MessagingConfigurationException(Error error, Exception innerException) : base(error, innerException)
    {
    }

    public MessagingConfigurationException(string message, Error error) : base(message, error)
    {
    }

    public MessagingConfigurationException(string message, Error error, Exception innerException) : base(message, error, innerException)
    {
    }

}