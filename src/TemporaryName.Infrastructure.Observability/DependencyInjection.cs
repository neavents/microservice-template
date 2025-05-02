using System;
using Microsoft.Extensions.DependencyInjection;

namespace TemporaryName.Infrastructure.Observability;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureObservability(this IServiceCollection services){

        return services;
    }
}
