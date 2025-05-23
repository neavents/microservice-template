using System;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.Caching.Abstractions;
using TemporaryName.Infrastructure.Caching.Abstractions.Constants;
using TemporaryName.Infrastructure.Caching.Abstractions.Settings;
using TemporaryName.Infrastructure.Caching.Memcached;
using TemporaryName.Infrastructure.Caching.Redis;

namespace TemporaryName.WebApi.Injections;

public static class CachingLayerInjection
{
    public static IServiceCollection AddExtensiveCaching(this IServiceCollection services,
    IConfiguration configuration, IOptions<CachingProvidersOptions> options,
    ILogger logger)
    {
        CachingProvidersOptions cachingOpts = options.Value;

        if (cachingOpts.ActiveProviders.Contains(CacheProviderKeys.Redis, StringComparer.OrdinalIgnoreCase))
        {
            services.AddInfrastructureCachingRedis(configuration, logger);
        }

        if (cachingOpts.ActiveProviders.Contains(CacheProviderKeys.Memcached, StringComparer.OrdinalIgnoreCase))
        {
            services.AddInfrastructureCachingMemcached(configuration, logger);
        }

        if (!string.IsNullOrWhiteSpace(cachingOpts.DefaultProvider) &&
    cachingOpts.ActiveProviders.Contains(cachingOpts.DefaultProvider, StringComparer.OrdinalIgnoreCase))
        {
            services.AddSingleton<ICacheService>(sp =>
            {
                // ILogger for this specific registration if needed
                // var localLogger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DefaultCacheProviderResolver");
                // localLogger.LogInformation("Resolving default cache provider: {DefaultProvider}", cachingProvidersSettings.DefaultProvider);
                return sp.GetRequiredKeyedService<ICacheService>(cachingOpts.DefaultProvider);
            });
        }
        else if (cachingOpts.ActiveProviders.Count > 0)
        {
            throw new InvalidOperationException("No DefaultProvider configured for ICacheService, but active providers exist. ICacheService will not be resolvable without a key.");
        }

        return services;
    }
}
