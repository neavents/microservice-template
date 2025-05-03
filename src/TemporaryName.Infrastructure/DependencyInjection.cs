using System;
using Microsoft.Extensions.DependencyInjection;
using TemporaryName.Infrastructure.Caching.Redis;
using TemporaryName.Infrastructure.ChangeDataCapture.Debezium;
using TemporaryName.Infrastructure.Configuration;
using TemporaryName.Infrastructure.HttpClient;
using TemporaryName.Infrastructure.Messaging.MassTransit;
using TemporaryName.Infrastructure.Observability;
using TemporaryName.Infrastructure.Persistence.Hybrid.Sql.PostgreSQL;
using TemporaryName.Infrastructure.Persistence.Seeding;
using TemporaryName.Infrastructure.Security.Auth.Keycloak;

namespace TemporaryName.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services){
        services
                .AddPersistenceLayer()
                .AddCachingLayer()
                .AddHttpClientLayer()
                .AddMessagingLayer()
                .AddConfigurationLayer()
                .AddChangeDataCaptureLayer()
                .AddObservabilityLayer()
                .AddSecurityLayer();
                
        return services;
    }

    private static IServiceCollection AddPersistenceLayer(this IServiceCollection services){
        services.AddInfrastructurePersistenceHybridSqlPostgreSQL()
                .AddInfrastructurePersistenceSeeding();

        return services;
    }

    private static IServiceCollection AddCachingLayer(this IServiceCollection services){
        services.AddInfrastructureCachingRedis();

        return services;
    }

    private static IServiceCollection AddHttpClientLayer(this IServiceCollection services){
        services.AddInfrastructureHttpClient();

        return services;
    }

    private static IServiceCollection AddMessagingLayer(this IServiceCollection services){
        services.AddInfrastructureMessagingMassTransit();

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

    private static IServiceCollection AddObservabilityLayer(this IServiceCollection services){
        services.AddInfrastructureObservability();

        return services;
    }

    private static IServiceCollection AddSecurityLayer(this IServiceCollection services){
        services.AddInfrastructureSecurityAuthKeycloak();

        return services;
    }

}
