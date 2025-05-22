using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.Messaging.MassTransit.Exceptions;
using TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators.RabbitMQ;

public static class EndpointConcurrencyExtensions
{
    /// <summary>
    /// Configures concurrency limits (ConcurrentMessageLimit, PrefetchCount) for a RabbitMQ receive endpoint.
    /// </summary>
    public static void ConfigureConcurrencyLimits(
        this IRabbitMqReceiveEndpointConfigurator endpointConfigurator,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        EndpointConcurrencyOptions? settings = serviceProvider
            .GetRequiredService<IOptionsMonitor<EndpointConcurrencyOptions>>()?.CurrentValue;

        if (settings is null)
        {
            Error error = new("ConfigurationError", $"{nameof(EndpointConcurrencyOptions)} is not configured, maybe there is missing json settings in masstransitsettings.json?");
            throw new NotConfiguredException(nameof(EndpointConcurrencyOptions), error);
        }

        if (settings.ConcurrentMessageLimit.HasValue)
        {
            endpointConfigurator.ConcurrentMessageLimit = settings.ConcurrentMessageLimit.Value;
        }
    }
}
