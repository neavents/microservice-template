using System;
using System.Reflection;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.Extensions.DependencyInjection;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators.RabbitMQ;

public static class RabbitMqBusFactoryExtensions
{
    private const int QueryMessageLimit = 50;
    private const int QueryDelay = 5;
    private const int DuplicateDetectionWindow = 30;
    private const int MessageDeliveryLimit = 100;
    private const int MessageDeliveryTimeout = 30;
    private const int PublisherConfirmation = 20;
    /// <summary>
    /// Centralized extension to configure the RabbitMQ bus with all modular components.
    /// </summary>
    public static IServiceCollection AddConfiguredMassTransit(
        this IServiceCollection services,
        Action<IServiceCollectionBusConfigurator> configureConsumersAndSagas,
        Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? configureRabbitMqBusFeatures = null
        )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureConsumersAndSagas);

        services.AddMassTransit(busCfg =>
        {
            busCfg.SetKebabCaseEndpointNameFormatter();

            configureConsumersAndSagas?.Invoke(busCfg);

            busCfg.AddEntityFrameworkOutbox<YourDbContext>(outboxConfigurator =>
            {
                outboxConfigurator.QueryMessageLimit = QueryMessageLimit;
                outboxConfigurator.QueryDelay = TimeSpan.FromSeconds(QueryDelay);
                outboxConfigurator.DuplicateDetectionWindow = TimeSpan.FromMinutes(DuplicateDetectionWindow);

                outboxConfigurator.UsePostgres();
                outboxConfigurator.LockStatementProvider = new PostgresLockStatementProvider();

                outboxConfigurator.UseBusOutbox(busOutboxConfigurator =>
                {
                    busOutboxConfigurator.MessageDeliveryLimit = MessageDeliveryLimit;
                    busOutboxConfigurator.MessageDeliveryTimeout = TimeSpan.FromSeconds(MessageDeliveryTimeout);
                });

            });

            busCfg.UsingRabbitMq((busContext, rabbitBusCfg) =>
            {
                rabbitBusCfg.ConfigureRabbitMqConnection(busContext.GetRequiredService<IServiceProvider>());
                rabbitBusCfg.ConfigureProtobufSerialization();
                rabbitBusCfg.ConfigurePublisherConfirmations(TimeSpan.FromSeconds(PublisherConfirmation));

                rabbitBusCfg.ConfigureGlobalErrorHandling(busContext.GetRequiredService<IServiceProvider>());
                rabbitBusCfg.ConfigureRabbitMqMessageScheduler();

                configureRabbitMqBusFeatures?.Invoke(busContext, rabbitBusCfg);

                rabbitBusCfg.ConfigureEndpoints(busContext);
            });
        });

        return services;
    }

    public static IServiceCollection AddConfiguredMassTransitWithAssemblyScanning(
        this IServiceCollection services,
        Assembly[] consumerAssemblies,
        Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? configureRabbitMqBusFeatures = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(consumerAssemblies);


        return services.AddConfiguredMassTransit(
            busRegistrationConfigurator =>
            {
                if (consumerAssemblies is not null && consumerAssemblies.Length > 0)
                {
                busRegistrationConfigurator.AddConsumers(consumerAssemblies);
                // busRegistrationConfigurator.AddSagas(consumerAssemblies); 
                // busRegistrationConfigurator.AddActivities(consumerAssemblies); 
                }
            },
                configureRabbitMqBusFeatures
        );
    }
}