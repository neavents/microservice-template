using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.Messaging.MassTransit.Exceptions;
using TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators.RabbitMQ;

public static class EndpointConsumerTimeoutExtensions
{
    /// <summary>
    /// Configures consumer execution timeouts for a RabbitMQ receive endpoint.
    /// </summary>
    public static void ConfigureConsumerTimeout(
        this IReceiveEndpointConfigurator endpointConfigurator,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        EndpointConsumerTimeoutOptions? settings = serviceProvider
            .GetRequiredService<IOptionsMonitor<EndpointConsumerTimeoutOptions>>()?.CurrentValue;
            
        if (settings is null)
        {
            Error error = new("ConfigurationError", $"{nameof(EndpointConsumerTimeoutOptions)} is not configured, maybe there is missing json settings in masstransitsettings.json?");
            throw new NotConfiguredException(nameof(EndpointConsumerTimeoutOptions), error);
        }

        if (!settings.Enabled)
        {
            return; 
        }

        if (settings.Timeout <= TimeSpan.Zero)
        {
            Error error = new("ConfigurationError", $"Specified Timeout ({settings.Timeout}) should be higher than 0.");
            throw new BadConfigurationException(nameof(EndpointConsumerTimeoutOptions), error);
        }

        endpointConfigurator.UseTimeout(t => t.Timeout = settings.Timeout);
    }
}
