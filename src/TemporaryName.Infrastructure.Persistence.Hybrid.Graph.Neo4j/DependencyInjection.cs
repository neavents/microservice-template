using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j.Abstractions;
using TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j.Implementations;
using TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j.Settings;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j;

public static partial class DependencyInjection // Made partial for logging
{
    public static IServiceCollection AddNeo4jPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Resolve logger for DI process itself
        var tempSp = services.BuildServiceProvider();
        var diLogger = tempSp.GetService<ILoggerFactory>()?.CreateLogger(typeof(DependencyInjection).FullName!)
            ?? throw new InvalidOperationException("ILoggerFactory not available for Neo4j DI setup.");

        LogStartingRegistration(diLogger); // Logging

        // Configure and validate options
        services.AddOptions<Neo4jOptions>()
            .Bind(configuration.GetSection(Neo4jOptions.SectionName))
            .ValidateDataAnnotations() // Requires Microsoft.Extensions.Options.DataAnnotations
            .ValidateOnStart();       // Validates options at application startup

        LogOptionsConfigured(diLogger, nameof(Neo4jOptions), Neo4jOptions.SectionName); // Logging

        // Register the INeo4jClientProvider as a singleton. The driver itself is thread-safe
        // and designed to be long-lived.
        services.AddSingleton<INeo4jClientProvider, Neo4jClientProvider>();
        LogClientProviderRegistered(diLogger, nameof(INeo4jClientProvider), nameof(Neo4jClientProvider), "Singleton"); // Logging

        // Register generic repositories or specific ones here if desired.
        // Example: services.AddScoped(typeof(IGraphRepository<>), typeof(Neo4jGenericRepository<>));

        LogRegistrationCompleted(diLogger); // Logging
        return services;
    }
}
