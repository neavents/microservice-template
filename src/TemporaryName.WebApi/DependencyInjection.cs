using System;
using TemporaryName.Application;
using TemporaryName.Infrastructure;

namespace TemporaryName.WebApi;

public static class DependencyInjection
{
    public static IServiceCollection AddLayers(this IServiceCollection services) {
        services.AddInfrastructureLayer()
                .AddApplicationLayer();

        return services;
    }
}
