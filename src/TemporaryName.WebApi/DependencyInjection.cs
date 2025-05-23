using System;
using TemporaryName.Application;
using TemporaryName.Infrastructure;
using TemporaryName.WebApi.Injections;
namespace TemporaryName.WebApi;

public static class DependencyInjection
{
    public static IServiceCollection AddLayers(this IServiceCollection services, ILogger logger, IConfiguration configuration) {
        services.AddInfrastructureLayer()
                .AddApplicationLayer();
        services.AddExtensiveCaching(configuration, null, logger);

        return services;
    }
}
