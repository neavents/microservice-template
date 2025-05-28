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
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var tempSp = services.BuildServiceProvider();
        var diLogger = tempSp.GetService<ILoggerFactory>()?.CreateLogger(typeof(DependencyInjection).FullName!)
            ?? throw new InvalidOperationException("ILoggerFactory not available for Cassandra DI setup.");

        LogStartingRegistration(diLogger);

        services.AddOptions<CassandraOptions>()
            .Bind(configuration.GetSection(CassandraOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        LogOptionsConfigured(diLogger, nameof(CassandraOptions), CassandraOptions.SectionName); 

        services.AddSingleton<ICassandraSessionProvider, CassandraSessionProvider>();
        LogSessionProviderRegistered(diLogger, nameof(ICassandraSessionProvider), nameof(CassandraSessionProvider), "Singleton");

        // Register repositories
        // services.AddScoped(typeof(ICassandraRepository<>), typeof(CassandraGenericRepository<>));

        LogRegistrationCompleted(diLogger); 
        return services;
    }
}
