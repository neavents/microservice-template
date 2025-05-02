using System;
using Microsoft.Extensions.DependencyInjection;

namespace TemporaryName.Infrastructure.Caching.Redis;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureCachingRedis(this IServiceCollection services){

        return services;
    }
}
