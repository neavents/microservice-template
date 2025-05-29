using System;
using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using TemporaryName.Infrastructure.Caching.Memcached;
using TemporaryName.Infrastructure.Caching.Redis;
using TemporaryName.Infrastructure.ChangeDataCapture.Debezium;
using TemporaryName.Infrastructure.Configuration;
using TemporaryName.Infrastructure.HttpClient;
using TemporaryName.Infrastructure.Messaging.MassTransit;
using TemporaryName.Infrastructure.Observability;
using TemporaryName.Infrastructure.Observability.Models;
using TemporaryName.Infrastructure.Observability.Settings;
using TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j;
using TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra;
using TemporaryName.Infrastructure.Persistence.Hybrid.Olap.ClickHouseDb;
using TemporaryName.Infrastructure.Persistence.Hybrid.Sql.PostgreSQL;
using TemporaryName.Infrastructure.Persistence.Seeding;
using TemporaryName.Infrastructure.Security.Auth.Keycloak;
using TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace TemporaryName.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, InfrastructureModel model){
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(model);

        services
                .AddPersistenceLayer(model.Configuration, model.Logger)
                .AddCachingLayer(model.Configuration, model.Logger)
                .AddHttpClientLayer()
                .AddMessagingLayer(model.Configuration, model.MassTransitConsumerAssemblies, model.ConfigureExtraRabbitMqBusFeatures)
                .AddConfigurationLayer()
                .AddChangeDataCaptureLayer()
                .AddObservabilityLayer(model.Configuration, model.ObservabilityOptions, model.Logger)
                .AddSecurityLayer(model.Configuration, model.Logger);
                
        return services;
    }

    private static IServiceCollection AddPersistenceLayer(this IServiceCollection services, IConfiguration configuration, ILogger logger){
        services.AddPostgreSqlPersistence(configuration, logger)
                .AddClickHousePersistence(configuration, logger)
                .AddNeo4jPersistence(configuration, logger)
                .AddCassandraPersistence(configuration, logger);
                //.AddInfrastructurePersistenceSeeding();

        return services;
    }

    private static IServiceCollection AddCachingLayer(this IServiceCollection services, IConfiguration configuration, ILogger logger){
        services.AddInfrastructureCachingRedis(configuration, logger)
                .AddInfrastructureCachingMemcached(configuration, logger);

        return services;
    }

    private static IServiceCollection AddHttpClientLayer(this IServiceCollection services){
        services.AddInfrastructureHttpClient();

        return services;
    }

    private static IServiceCollection AddMessagingLayer(this IServiceCollection services, IConfiguration configuration, Assembly[] assemblies, Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? configureExtraRabbitMqBusFeatures = null){
        services.AddMassTransitLayer(configuration, assemblies, configureExtraRabbitMqBusFeatures);

        return services;
    }

    private static IServiceCollection AddConfigurationLayer(this IServiceCollection services){
        services.AddInfrastructureConfiguration();

        return services;
    }

    private static IServiceCollection AddChangeDataCaptureLayer(this IServiceCollection services){
        services.AddInfrastructureChangeDataCaptureDebezium();

        return services;
    }

    private static IServiceCollection AddObservabilityLayer(this IServiceCollection services, IConfiguration configuration,  ObservabilityOptions observabilityOptions, ILogger logger){
        
        services.AddConfiguredAppElasticApm(configuration, observabilityOptions, logger);

        return services;
    }

    private static IServiceCollection AddSecurityLayer(this IServiceCollection services, IConfiguration configuration, ILogger logger){
        services.AddInfrastructureSecurityAuthKeycloak()
                .AddHashiCorpVaultSecrets(configuration, logger);

        return services;
    }

}
