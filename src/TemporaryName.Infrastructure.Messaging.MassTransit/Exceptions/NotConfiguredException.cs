using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Exceptions;

public class NotConfiguredException : MessagingConfigurationException
{
    public string ConfigurationName;

    public NotConfiguredException(string configurationName, Error error) : base(error)
    {
        ConfigurationName = configurationName;
    }

    public NotConfiguredException(string configurationName, Error error, Exception innerException) : base(error, innerException)
    {
        ConfigurationName = configurationName;
    }

    public NotConfiguredException(string configurationName, string message, Error error) : base(message, error)
    {
        ConfigurationName = configurationName;
    }

    public NotConfiguredException(string configurationName, string message, Error error, Exception innerException) : base(message, error, innerException)
    {
        ConfigurationName = configurationName;
    }
}
