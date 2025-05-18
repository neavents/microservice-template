using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Implementations;
using TemporaryName.Infrastructure.MultiTenancy.Implementations.Providers;
using TemporaryName.Infrastructure.MultiTenancy.Middlewares;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy;

    public static class DependencyInjection
    {
        private static void LogMessage(IServiceCollection? services, LogLevel level, string message)
        {
            ILogger? logger = null;
            if (services != null)
            {
                var serviceProvider = services.BuildServiceProvider();
                logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(DependencyInjection).FullName!);
            }

            if (logger == null)
            {
                using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
                logger = loggerFactory.CreateLogger(typeof(DependencyInjection).FullName!);
            }
            logger.Log(level, message);
        }

        public static IServiceCollection AddMultiTenancy(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services, nameof(services));
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

            LogMessage(services, LogLevel.Information, "Starting MultiTenancy core services registration.");

            services.AddOptions<MultiTenancyOptions>()
                .Bind(configuration.GetSection(MultiTenancyOptions.ConfigurationSectionName))
                .ValidateDataAnnotations() 
                .ValidateOnStart();       

            services.TryAddSingleton<MultiTenancyOptions>(sp =>
                sp.GetRequiredService<IOptionsMonitor<MultiTenancyOptions>>().CurrentValue);
            LogMessage(services, LogLevel.Debug, "MultiTenancyOptions configured and registered directly.");

            services.TryAddScoped<ITenantContext, TenantContext>();
            LogMessage(services, LogLevel.Debug, "ITenantContext registered as TenantContext (Scoped).");

            services.TryAddSingleton<ITenantStrategyProvider, TenantStrategyProvider>();
            LogMessage(services, LogLevel.Debug, "ITenantStrategyProvider registered as TenantStrategyProvider (Singleton).");

            services.TryAddSingleton<ITenantStoreProvider, TenantStoreProvider>();
            LogMessage(services, LogLevel.Debug, "ITenantStoreProvider registered as TenantStoreProvider (Singleton).");


            services.AddMemoryCache(); 
            services.AddHttpClient();  
            LogMessage(services, LogLevel.Debug, "Essential services (IMemoryCache, IHttpClientFactory) ensured.");


            services.TryAddScoped<ITenantStore>(serviceProvider =>
            {
                var storeProvider = serviceProvider.GetRequiredService<ITenantStoreProvider>();
                var options = serviceProvider.GetRequiredService<MultiTenancyOptions>();
                var logger = serviceProvider.GetRequiredService<ILogger<ITenantStore>>(); 

                logger.LogDebug("Resolving ITenantStore via factory method using TenantStoreProvider. Store Type from options: {StoreType}", options.Store.Type);
                ITenantStore store = storeProvider.GetStore(options.Store);
                logger.LogInformation("ITenantStore resolved to type {ActualStoreType} (based on configuration {ConfiguredStoreType}).", store.GetType().Name, options.Store.Type);
                return store;
            });
            LogMessage(services, LogLevel.Debug, "Base ITenantStore registered (Scoped) via factory method.");


            LogMessage(services, LogLevel.Information, "MultiTenancy core services registration completed.");
            return services;
        }

        public static IApplicationBuilder UseMultiTenancy(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app, nameof(app));

            ILogger logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>()
                                .CreateLogger(typeof(DependencyInjection).FullName!);

            app.UseMiddleware<TenantResolutionMiddleware>();
            logger.LogInformation("TenantResolutionMiddleware registered in the ASP.NET Core request pipeline.");
            return app;
        }
    }
