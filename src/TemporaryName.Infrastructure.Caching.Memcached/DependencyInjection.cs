using System;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.Caching.Abstractions;
using TemporaryName.Infrastructure.Caching.Abstractions.Constants;
using TemporaryName.Infrastructure.Caching.Memcached.Implementations;
using TemporaryName.Infrastructure.Caching.Memcached.Settings;

namespace TemporaryName.Infrastructure.Caching.Memcached;

public static partial class DependencyInjection
{
    public static IServiceCollection AddInfrastructureCachingMemcached(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        LogStartingRegistration(logger);

        services.AddOptions<MemcachedCacheOptions>()
            .Bind(configuration.GetSection(MemcachedCacheOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        // Log options after they are available via IOptions
        services.AddSingleton(sp => {
            var options = sp.GetRequiredService<IOptions<MemcachedCacheOptions>>().Value;
            LogOptionsConfigured(logger, nameof(MemcachedCacheOptions), MemcachedCacheOptions.SectionName, options.Servers.Count);
            return options; // Though not strictly needed to return if only for logging
        });


        // Register EnyimMemcached client
        services.AddEnyimMemcached(enyimOptions => {
// Resolve our strongly-typed options AFTER they've been configured and validated by AddOptions above.
            // This requires IOptions<MemcachedCacheOptions> to be resolvable when this lambda runs.
            // To ensure this, we resolve it from a temporary service provider if services haven't been built yet,
            // or better, ensure options are registered such that they can be resolved by AddEnyimMemcached's internal SP.
            // A common pattern is to resolve IConfiguration or IOptions directly if needed.
            // For simplicity, if MemcachedCacheOptions is registered as Singleton IOptions, it can be resolved.

            // It's safer to get our options from the outer scope or by building a temporary provider
            // if this lambda is truly deferred until after initial SP build.
            // However, AddEnyimMemcached's Action<MemcachedClientOptions> is typically called during the
            // initial AddEnyimMemcached call itself if it configures services immediately.
            // Let's assume IOptions<MemcachedCacheOptions> is resolvable.
            // This lambda is executed when IMemcachedClient is first requested, or sometimes earlier.
            // To ensure it works when this DI extension is called:
            var serviceProviderForOptions = services.BuildServiceProvider(validateScopes:true); // Build a temporary SP
            var customOptions = serviceProviderForOptions.GetRequiredService<IOptions<MemcachedCacheOptions>>().Value;
            // Dispose the temporary SP if it's causing issues, or ensure options are available another way.
            // A cleaner way if AddEnyimMemcached has an overload that takes IConfiguration:
            // services.AddEnyimMemcached(configuration.GetSection(MemcachedCacheOptions.SectionName));
            // But to use our custom options class directly:

            LogOptionsConfigured(logger, nameof(MemcachedCacheOptions), MemcachedCacheOptions.SectionName, customOptions.Servers.Count);

            if (customOptions.Servers is null || customOptions.Servers.Count == 0)
            {
                string errorMsg = "No Memcached servers configured in MemcachedCacheOptions.";
                LogMemcachedClientConfigError(logger, errorMsg, null);
                throw new InvalidOperationException(errorMsg);
            }

            foreach (string server in customOptions.Servers)
            {
                enyimOptions.AddServer(server); // Enyim's AddServer can parse "host:port"
            }

            if (!string.IsNullOrWhiteSpace(customOptions.Username) && !string.IsNullOrWhiteSpace(customOptions.Password))
            {
                enyimOptions.Authentication.Type = typeof(PlainTextAuthenticator).ToString();
                enyimOptions.Authentication.Parameters.Add("userName", customOptions.Username);
                enyimOptions.Authentication.Parameters.Add("password", customOptions.Password);
                if (!string.IsNullOrWhiteSpace(customOptions.Zone))
                {
                    enyimOptions.Authentication.Parameters.Add("zone", customOptions.Zone);
                }
            }
        });


        
        services.AddSingleton<MemcachedCacheKeyService>();
        LogCacheKeyServiceRegistered(logger, nameof(ICacheKeyService), nameof(MemcachedCacheKeyService), "Singleton");
        services.AddKeyedSingleton<ICacheKeyService, MemcachedCacheKeyService>(CacheProviderKeys.Memcached);

        services.AddSingleton<MemcachedCacheService>(); // Concrete type
        services.AddKeyedSingleton<ICacheService, MemcachedCacheService>(CacheProviderKeys.Memcached); // Keyed service
        LogCacheServiceRegistered(logger, nameof(ICacheService), CacheProviderKeys.Memcached, nameof(MemcachedCacheService), "Singleton (Keyed)");

        LogRegistrationCompleted(logger);
        return services;
    }
}
