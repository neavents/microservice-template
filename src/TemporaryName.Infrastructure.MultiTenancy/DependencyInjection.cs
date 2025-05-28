using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;
using TemporaryName.Infrastructure.MultiTenancy.Implementations;
using TemporaryName.Infrastructure.MultiTenancy.Implementations.Factories;
using TemporaryName.Infrastructure.MultiTenancy.Implementations.Providers;
using TemporaryName.Infrastructure.MultiTenancy.Implementations.Stores;
using TemporaryName.Infrastructure.MultiTenancy.Implementations.Strategies;
using TemporaryName.Infrastructure.MultiTenancy.Middlewares;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy;

public static partial class DependencyInjection
{
    private static ILogger? _logger;

    private static void EnsureLoggerInitialized(IServiceProvider? serviceProvider = null)
    {
        if (_logger is null)
        {
            using var loggerFactory = serviceProvider?.GetService<ILoggerFactory>() ?? LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
            _logger = loggerFactory.CreateLogger(typeof(DependencyInjection).FullName!);
        }
    }

    public static IServiceCollection AddMultiTenancy(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MultiTenancyOptions>? configureMultiTenancyOptions = null,
        Action<TenantDataOptions>? configureTenantDataOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        EnsureLoggerInitialized(services.BuildServiceProvider());

        LogStartingRegistration(_logger);

        services.AddOptions<MultiTenancyOptions>()
            .Bind(configuration.GetSection(MultiTenancyOptions.ConfigurationSectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (configureMultiTenancyOptions != null) services.Configure(configureMultiTenancyOptions);

        LogConfiguredFrom(_logger, nameof(MultiTenancyOptions), MultiTenancyOptions.ConfigurationSectionName);

        services.AddOptions<TenantDataOptions>()
                .Bind(configuration.GetSection(TenantDataOptions.ConfigurationSectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        if (configureTenantDataOptions != null) services.Configure(configureTenantDataOptions);

        LogConfiguredFrom(_logger, nameof(TenantDataOptions), TenantDataOptions.ConfigurationSectionName);


        services.TryAddSingleton<MultiTenancyOptions>(sp =>
            sp.GetRequiredService<IOptionsMonitor<MultiTenancyOptions>>().CurrentValue);
        LogConfiguredAndRegistered(_logger, nameof(MultiTenancyOptions));

        services.TryAddScoped<ITenantContext, TenantContext>();
        LogRegisteredAs(_logger, nameof(ITenantContext), nameof(TenantContext), nameof(ServiceLifetime.Scoped));

        services.TryAddSingleton<ITenantStrategyProvider, TenantStrategyProvider>();
        LogRegisteredAs(_logger, nameof(ITenantStrategyProvider), nameof(TenantStrategyProvider), nameof(ServiceLifetime.Singleton));

        services.TryAddSingleton<ITenantStoreProvider, TenantStoreProvider>();
        LogRegisteredAs(_logger, nameof(ITenantStoreProvider), nameof(TenantStoreProvider), nameof(ServiceLifetime.Singleton));

        services.TryAddScoped<ITenantOperationScopeFactory, TenantOperationScopeFactory>();
        LogRegisteredAs(_logger, nameof(ITenantOperationScopeFactory), nameof(TenantOperationScopeFactory), nameof(ServiceLifetime.Scoped));


        services.TryAddTransient<HostHeaderTenantIdentificationStrategy>();
        LogRegisteredAs(_logger, nameof(HostHeaderTenantIdentificationStrategy), "Self", nameof(ServiceLifetime.Transient));

        services.TryAddTransient<HttpHeaderTenantIdentificationStrategy>();
        LogRegisteredAs(_logger, nameof(HttpHeaderTenantIdentificationStrategy), "Self", nameof(ServiceLifetime.Transient));

        services.TryAddTransient<QueryStringTenantIdentificationStrategy>();
        LogRegisteredAs(_logger, nameof(QueryStringTenantIdentificationStrategy), "Self", nameof(ServiceLifetime.Transient));

        services.TryAddTransient<RouteValueTenantIdentificationStrategy>();
        LogRegisteredAs(_logger, nameof(RouteValueTenantIdentificationStrategy), "Self", nameof(ServiceLifetime.Transient));

        services.TryAddTransient<ClaimTenantIdentificationStrategy>();
        LogRegisteredAs(_logger, nameof(ClaimTenantIdentificationStrategy), "Self", nameof(ServiceLifetime.Transient));


        services.TryAddScoped<ConfigurationTenantStore>();
        LogRegisteredAs(_logger, nameof(ConfigurationTenantStore), "Self", nameof(ServiceLifetime.Scoped));

        services.TryAddScoped<DatabaseTenantStore>();
        LogRegisteredAs(_logger, nameof(DatabaseTenantStore), "Self", nameof(ServiceLifetime.Scoped));

        services.TryAddScoped<RemoteHttpTenantStore>();
        LogRegisteredAs(_logger, nameof(RemoteHttpTenantStore), "Self", nameof(ServiceLifetime.Scoped));

        services.TryAddScoped<InMemoryTenantStore>();
        LogRegisteredAs(_logger, nameof(InMemoryTenantStore), "Self", nameof(ServiceLifetime.Scoped));

        services.AddMemoryCache();
        services.AddHttpClient();
        LogEssentialServicesEnsured(_logger, "IMemoryCache and IHttpClientFactory with HttpClient things");


        services.TryAddScoped<ITenantStore>(serviceProvider =>
        {
            EnsureLoggerInitialized(serviceProvider);

            var storeProvider = serviceProvider.GetRequiredService<ITenantStoreProvider>();
            var options = serviceProvider.GetRequiredService<MultiTenancyOptions>();

            LogResolvingViaFactoryMethod(_logger, nameof(ITenantStore), nameof(TenantStoreProvider), nameof(options.Store.Type));

            ITenantStore store = storeProvider.GetStore(options.Store);
            LogResolvedToType(_logger, nameof(ITenantStore), store.GetType().Name, nameof(options.Store.Type));

            return store;
        });

        LogBaseRegisteredViaFactory(_logger, nameof(ITenantStore), nameof(ServiceLifetime.Scoped));

        LogRegistrationCompleted(_logger, nameof(MultiTenancy));
        return services;
    }

    public static IApplicationBuilder UseMultiTenancy(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        EnsureLoggerInitialized(app.ApplicationServices);

        app.UseMiddleware<TenantResolutionMiddleware>();

        LogRegisteredIn(_logger, nameof(TenantResolutionMiddleware));
        return app;
    }
}
