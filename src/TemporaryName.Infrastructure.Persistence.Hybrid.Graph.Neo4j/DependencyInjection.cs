using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j.Abstractions;
using TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j.Implementations;
using TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j.Settings;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j;

public static partial class DependencyInjection
{
    public static IServiceCollection AddNeo4jPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var tempSp = services.BuildServiceProvider();

        LogStartingRegistration(logger);

        services.AddOptions<Neo4jOptions>()
            .Bind(configuration.GetSection(Neo4jOptions.SectionName))
            .ValidateDataAnnotations() 
            .ValidateOnStart();       

        LogOptionsConfigured(logger, nameof(Neo4jOptions), Neo4jOptions.SectionName);

        services.AddSingleton<INeo4jClientProvider, Neo4jClientProvider>();
        LogClientProviderRegistered(logger, nameof(INeo4jClientProvider), nameof(Neo4jClientProvider), "Singleton");

        // Register generic repositories or specific ones here if desired.
        // services.AddScoped(typeof(IGraphRepository<>), typeof(Neo4jGenericRepository<>));

        LogRegistrationCompleted(logger);
        return services;
    }
}
