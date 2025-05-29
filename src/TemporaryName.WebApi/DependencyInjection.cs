using System;
using TemporaryName.Application;
using TemporaryName.Domain;
using TemporaryName.Infrastructure;
using TemporaryName.Infrastructure.Observability.Models;
using TemporaryName.Infrastructure.Observability.Settings;
using TemporaryName.WebApi.Injections;
namespace TemporaryName.WebApi;

public static class DependencyInjection
{
    public static IServiceCollection AddLayers(this IServiceCollection services, ILogger logger, WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(builder);


        InfrastructureModel infrastructureModel = new()
        {
            Logger = logger,
            Configuration = builder.Configuration,
            MassTransitConsumerAssemblies = [],
            ConfigureExtraRabbitMqBusFeatures = null,
            ObservabilityOptions = builder.Configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>()!
        };

        services.AddInfrastructureLayer(infrastructureModel)
                .AddApplicationLayer()
                .AddDomainLayer();
        //services.AddExtensiveCaching(configuration, null, logger);

        return services;
    }

    public static WebApplication AddMiddlewaresfromLayers(this WebApplication app)
    {
        app.AddInfrastructureMiddlewares();
        return app;
    }
}
