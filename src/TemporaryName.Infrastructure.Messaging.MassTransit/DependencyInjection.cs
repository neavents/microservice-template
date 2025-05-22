using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TemporaryName.Infrastructure.Messaging.MassTransit.Configurators.RabbitMQ;
using TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

namespace TemporaryName.Infrastructure.Messaging.MassTransit;

public static partial class DependencyInjection
{
    /// <summary>
    /// Adds and configures the MassTransit layer with all its components,
    /// including RabbitMQ, error handling, outbox, endpoint features, and health checks.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configuration">The application's IConfiguration instance.</param>
    /// <param name="consumerAssemblies">Assemblies to scan for MassTransit Consumers, Sagas, etc.</param>
    /// <param name="configureExtraRabbitMqBusFeatures">Optional action for further RabbitMQ bus-specific configurations.</param>
    /// <returns>The IServiceCollection for chaining.</returns>
    public static IServiceCollection AddMassTransitLayer(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly[] consumerAssemblies,
        Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? configureExtraRabbitMqBusFeatures = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<RabbitMqConnectionOptions>(
            configuration.GetSection(RabbitMqConnectionOptions.SectionName)
        );
        services.Configure<RabbitMqHealthCheckOptions>(
            configuration.GetSection(RabbitMqHealthCheckOptions.SectionName)
        );
        services.Configure<GlobalErrorHandlingOptions>(
            configuration.GetSection(GlobalErrorHandlingOptions.SectionName)
        );
        services.Configure<EndpointConcurrencyOptions>(
            configuration.GetSection(EndpointConcurrencyOptions.SectionName)
        );
        services.Configure<EndpointCircuitBreakerOptions>(
            configuration.GetSection(EndpointCircuitBreakerOptions.SectionName)
        );
        services.Configure<EndpointRateLimiterOptions>(
            configuration.GetSection(EndpointRateLimiterOptions.SectionName)
        );
        services.Configure<EndpointConsumerOutboxOptions>(
            configuration.GetSection(EndpointConsumerOutboxOptions.SectionName)
        );
        services.Configure<EndpointConsumerTimeoutOptions>(
            configuration.GetSection(EndpointConsumerTimeoutOptions.SectionName)
        );
        services.Configure<EndpointQuorumQueueOptions>(
            configuration.GetSection(EndpointQuorumQueueOptions.SectionName)
        );

        services.AddConfiguredMassTransitWithAssemblyScanning(
           consumerAssemblies: consumerAssemblies,
           configureRabbitMqBusFeatures: configureExtraRabbitMqBusFeatures
        );

        services.AddDedicatedRabbitMqHealthChecks();

        return services;
    }
}


