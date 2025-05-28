// src/TemporaryName.Infrastructure.Outbox.EFCore/DependencyInjection.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; // For ILogger (used by interceptor and for DI validation logging)
using Microsoft.Extensions.Options; // For IOptions validation/access pattern
using TemporaryName.Infrastructure.Outbox.EFCore.Interceptors;
using TemporaryName.Infrastructure.Outbox.EFCore.Services;
using TemporaryName.Infrastructure.Outbox.EFCore.Settings;

namespace TemporaryName.Infrastructure.Outbox.EFCore;

public static class DependencyInjection
{
    /// <summary>
    /// Adds services related to the EF Core Transactional Outbox pattern.
    /// This includes:
    /// 1. The <see cref="ConvertDomainEventsToOutboxMessagesInterceptor"/> for atomically saving domain events.
    /// 2. The <see cref="OutboxEventRelayService"/> as a hosted background service for relaying these events.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configuration">The application configuration, used to retrieve <see cref="OutboxEventRelaySettings"/>.</param>
    /// <returns>The IServiceCollection for chaining, allowing further service registrations.</returns>
    /// <remarks>
    /// This method assumes that the application's main DbContext (where business entities and outbox messages are stored)
    /// is registered elsewhere in the DI container. The <see cref="ConvertDomainEventsToOutboxMessagesInterceptor"/>
    /// must be added to that DbContext's options. Use the <see cref="AddOutboxSaveChangesInterceptor"/>
    /// extension method on <see cref="DbContextOptionsBuilder"/> for convenient registration.
    ///
    /// Critical dependencies expected to be registered by other infrastructure projects:
    /// - <see cref="TemporaryName.Infrastructure.Outbox.Abstractions.IOutboxMessageSource"/> (e.g., implemented by the Debezium CDC project).
    /// - <see cref="TemporaryName.Application.Contracts.Abstractions.Messaging.IIntegrationEventPublisher"/> (e.g., implemented by the Messaging.MassTransit project).
    /// - Standard logging services (ILoggerFactory, ILogger&lt;T&gt;).
    /// </remarks>
    public static IServiceCollection AddTransactionalOutboxEFCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // 1. Register the SaveChangesInterceptor.
        // This interceptor is responsible for converting domain events from aggregates into OutboxMessage entities.
        // It's registered as Singleton because interceptors are generally stateless. EF Core resolves and uses
        // this single instance for all DbContext instances it's attached to.
        // It requires ILogger<ConvertDomainEventsToOutboxMessagesInterceptor>.
        services.AddSingleton<ConvertDomainEventsToOutboxMessagesInterceptor>();
        // Ensure logging infrastructure is available for the interceptor and other services.
        services.AddLogging();

        // 2. Configure and Register the OutboxEventRelayService.
        // Bind the OutboxEventRelaySettings from configuration.
        IConfigurationSection relaySettingsSection = configuration.GetSection(OutboxEventRelaySettings.SectionName);
        services.Configure<OutboxEventRelaySettings>(relaySettingsSection);

        // Validate critical settings at startup to fail fast if misconfigured.
        // This uses IOptions validation pattern or direct checks.
        // For a "FAANG level" template, consider using IOptionsSnapshot<T>.Validate() with DataAnnotations or custom validation.
        OutboxEventRelaySettings? relaySettings = relaySettingsSection.Get<OutboxEventRelaySettings>();

        if (relaySettings == null)
        {
            // Log this issue using a temporary logger if service provider isn't fully built,
            // or rely on IOptions validation to throw later.
            // For immediate feedback during startup, this explicit check is useful.
            // This situation usually means the appsettings.json section is entirely missing.
            string errorMessage = $"CRITICAL CONFIGURATION ERROR: Configuration section '{OutboxEventRelaySettings.SectionName}' is missing. The OutboxEventRelayService cannot be configured and will not run. Ensure this section exists in your application settings.";
            
            // Attempt to get a logger to make this visible during startup.
            // This is a bit of a workaround as the main SP might not be fully built.
            var tempSp = services.BuildServiceProvider(); // Temporary SP, use with caution
            var startupLogger = tempSp.GetService<ILoggerFactory>()?.CreateLogger("TemporaryName.Infrastructure.Outbox.EFCore.Startup");
            startupLogger?.LogCritical(errorMessage);

            // Depending on the desired strictness, either throw to halt startup or allow proceeding with the service disabled.
            // For a core component like outbox relay, throwing is often safer if it's intended to be enabled.
            // However, if "Enabled: false" is a valid state, then just logging is okay.
            // The service constructor itself will throw if IOptions<OutboxEventRelaySettings>.Value is null.
            // For now, we let the constructor handle the null settings object.
        }

        // Register the OutboxEventRelayService as a hosted service (background worker)
        // only if it's explicitly enabled in the configuration.
        if (relaySettings?.Enabled == true)
        {
            // The OutboxEventRelayService has dependencies (IOutboxMessageSource, IIntegrationEventPublisher)
            // that MUST be registered in the DI container by other projects (e.g., Debezium project, MassTransit project).
            // If these are not registered, AddHostedService will cause a runtime failure when the host tries to create the service.
            services.AddHostedService<OutboxEventRelayService>();
            
            var tempSp = services.BuildServiceProvider();
            var serviceLogger = tempSp.GetService<ILoggerFactory>()?.CreateLogger("TemporaryName.Infrastructure.Outbox.EFCore.Startup");
            serviceLogger?.LogInformation("OutboxEventRelayService is ENABLED and has been registered as a hosted service. Instance log name: {RelayInstanceLogName}", relaySettings.RelayInstanceLogName);
        }
        else
        {
            // Log that the service is configured but disabled.
            // The service's constructor also logs this, but logging at DI registration time is also useful.
            var tempSp = services.BuildServiceProvider();
            var serviceLogger = tempSp.GetService<ILoggerFactory>()?.CreateLogger("TemporaryName.Infrastructure.Outbox.EFCore.Startup");
            serviceLogger?.LogInformation("OutboxEventRelayService is configured as DISABLED in section '{ConfigSectionName}'. It will not be started as a hosted service.", OutboxEventRelaySettings.SectionName);
        }

        return services;
    }

    /// <summary>
    /// Helper extension method to add the <see cref="ConvertDomainEventsToOutboxMessagesInterceptor"/>
    /// to a <see cref="DbContextOptionsBuilder"/>.
    /// This should be called when configuring your main application DbContext (e.g., in your Persistence project's DI setup).
    /// </summary>
    /// <param name="optionsBuilder">The <see cref="DbContextOptionsBuilder"/> to which the interceptor will be added.</param>
    /// <param name="serviceProvider">
    /// The <see cref="IServiceProvider"/> used to resolve the <see cref="ConvertDomainEventsToOutboxMessagesInterceptor"/>.
    /// This is typically available during DbContext configuration within `services.AddDbContext<TContext>((sp, options) => ...)`.
    /// </param>
    /// <returns>The <see cref="DbContextOptionsBuilder"/> for chaining, allowing further configuration.</returns>
    /// <exception cref="ArgumentNullException">If optionsBuilder or serviceProvider is null.</exception>
    public static DbContextOptionsBuilder AddOutboxSaveChangesInterceptor(
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        // Resolve the interceptor from the service provider.
        // It's registered as Singleton, so this will retrieve that instance.
        ConvertDomainEventsToOutboxMessagesInterceptor? interceptor = serviceProvider.GetService<ConvertDomainEventsToOutboxMessagesInterceptor>();

        if (interceptor != null)
        {
            optionsBuilder.AddInterceptors(interceptor);
            ILogger? logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("TemporaryName.Infrastructure.Outbox.EFCore.DbContextConfiguration");
            logger?.LogInformation("Successfully added ConvertDomainEventsToOutboxMessagesInterceptor to DbContextOptionsBuilder.");
        }
        else
        {
            // This indicates a DI setup issue if the interceptor was expected to be registered.
            ILogger? logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("TemporaryName.Infrastructure.Outbox.EFCore.DbContextConfiguration");
            logger?.LogWarning("ConvertDomainEventsToOutboxMessagesInterceptor could not be resolved from IServiceProvider. Transactional outbox functionality via this interceptor will be impaired. Ensure it is registered correctly (e.g., via AddTransactionalOutboxEFCore).");
            // Depending on strictness, you might throw an InvalidOperationException here.
        }
        return optionsBuilder;
    }
}
