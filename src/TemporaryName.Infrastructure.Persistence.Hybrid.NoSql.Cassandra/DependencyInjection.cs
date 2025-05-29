using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra.Abstractions;
using TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra.Implementations;
using TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra.Settings;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra;

public static partial class DependencyInjection 
{
    public static IServiceCollection AddCassandraPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var tempSp = services.BuildServiceProvider();
        
        LogStartingRegistration(logger);

        services.AddOptions<CassandraOptions>()
            .Bind(configuration.GetSection(CassandraOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        LogOptionsConfigured(logger, nameof(CassandraOptions), CassandraOptions.SectionName); 

        services.AddSingleton<ICassandraSessionProvider, CassandraSessionProvider>();
        LogSessionProviderRegistered(logger, nameof(ICassandraSessionProvider), nameof(CassandraSessionProvider), "Singleton");

        // Register repositories
        // services.AddScoped(typeof(ICassandraRepository<>), typeof(CassandraGenericRepository<>));

        LogRegistrationCompleted(logger); 
        return services;
    }
}
