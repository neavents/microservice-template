using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.Messaging.MassTransit.Exceptions;
using TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators.RabbitMQ;

public static class EndpointConsumerOutboxExtensions
{
    /// <summary>
    /// Configures the EF Core Transactional Outbox (Inbox pattern) for a RabbitMQ receive endpoint.
    /// This provides idempotent message processing for consumers.
    /// </summary>
    public static void ConfigureConsumerOutbox(
        this IReceiveEndpointConfigurator endpointConfigurator,
        IBusRegistrationContext registrationContext)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(registrationContext);

        EndpointConsumerOutboxOptions? settings = registrationContext
            .GetRequiredService<IOptionsMonitor<EndpointConsumerOutboxOptions>>()?.CurrentValue;

        if (settings is null)
        {
            Error error = new("ConfigurationError", $"{nameof(EndpointConsumerOutboxOptions)} is not configured, maybe there is missing json settings in masstransitsettings.json?");
            throw new NotConfiguredException(nameof(EndpointConsumerOutboxOptions), error);
        }

        if (!settings.Enabled)
        {
            return;
        }


        endpointConfigurator.UseEntityFrameworkOutbox<YourDbContext>(registrationContext);
    }
}
