using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators.RabbitMQ;

public static class EndpointFeaturesExtensions
    {
    /// <summary>
    /// Applies a standard set of resilience and performance features to a receive endpoint.
    /// This method will be called by MassTransit's ConfigureEndpoints for each discovered consumer/saga endpoint.
    /// </summary>
    public static void ConfigureEndpointFeatures(
        this IRabbitMqReceiveEndpointConfigurator endpointConfigurator,
        string endpointName,
        IBusRegistrationContext registrationContext)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(endpointName);
        ArgumentNullException.ThrowIfNull(registrationContext);

        IServiceProvider serviceProvider = registrationContext.GetRequiredService<IServiceProvider>();
        ArgumentNullException.ThrowIfNull(serviceProvider);
        
        ILogger logger = serviceProvider.GetRequiredService<ILogger>();
        ArgumentNullException.ThrowIfNull(logger);

        ConsumerRetryOptions retryOptions = serviceProvider.GetRequiredService<IOptionsMonitor<ConsumerRetryOptions>>().CurrentValue;
        ArgumentNullException.ThrowIfNull(retryOptions);
        
        endpointConfigurator.ConfigureConcurrencyLimits(serviceProvider);
        endpointConfigurator.ConfigureCircuitBreaker(serviceProvider);
        endpointConfigurator.ConfigureRateLimiter(serviceProvider);

        endpointConfigurator.ConfigureConsumerOutbox(registrationContext);
        endpointConfigurator.ConfigureConsumerTimeout(serviceProvider);

        endpointConfigurator.ConfigureQuorumQueue(endpointName, serviceProvider);
        RetryConfigurator.ConfigureConsumerRetries(endpointConfigurator, retryOptions, logger, "RABBITMQ");

    }
}
