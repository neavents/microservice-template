using System;
using MassTransit;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators.RabbitMQ;

public class CustomReceiveEndpointConfiguration : IConfigureReceiveEndpoint
{
    private readonly IBusRegistrationContext _busRegistrationContext;
    public CustomReceiveEndpointConfiguration(IBusRegistrationContext busRegistrationContext)
    {
        _busRegistrationContext = busRegistrationContext ?? throw new ArgumentNullException(nameof(busRegistrationContext));
    }

    public void Configure(string name, IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configurator);

        if (configurator is IRabbitMqReceiveEndpointConfigurator rabbitMqEndpointConfigurator)
        {
            rabbitMqEndpointConfigurator.ConfigureEndpointFeatures(name, _busRegistrationContext);
        }
        
    }
}
