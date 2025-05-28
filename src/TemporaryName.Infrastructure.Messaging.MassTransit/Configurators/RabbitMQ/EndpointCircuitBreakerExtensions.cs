using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.Messaging.MassTransit.Exceptions;
using TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators.RabbitMQ;

public static class EndpointCircuitBreakerExtensions
{
    /// <summary>
    /// Configures a circuit breaker for a RabbitMQ receive endpoint.
    /// </summary>
    public static void ConfigureCircuitBreaker(
        this IRabbitMqReceiveEndpointConfigurator endpointConfigurator,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        EndpointCircuitBreakerOptions? settings = serviceProvider
            .GetRequiredService<IOptionsMonitor<EndpointCircuitBreakerOptions>>()?.CurrentValue;
        
        if (settings is null)
        {
            Error error = new("ConfigurationError", $"{nameof(EndpointCircuitBreakerOptions)} is not configured, maybe there is missing json settings in masstransitsettings.json?");
            throw new NotConfiguredException(nameof(EndpointCircuitBreakerOptions), error);
        }
        
        if (!settings.Enabled)
        {
            return;
        }

        endpointConfigurator.UseCircuitBreaker(cb =>
        {
            cb.TrackingPeriod = settings.TrackingPeriod;
            cb.TripThreshold = settings.TripThreshold;
            cb.ActiveThreshold = settings.ActiveThreshold;
            cb.ResetInterval = settings.ResetInterval;
        });
    }
}
