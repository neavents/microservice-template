using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.Persistence.Hybrid.Olap.ClickHouseDb.Abstractions;
using TemporaryName.Infrastructure.Persistence.Hybrid.Olap.ClickHouseDb.Implementations;
using TemporaryName.Infrastructure.Persistence.Hybrid.Olap.ClickHouseDb.Settings;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Olap.ClickHouseDb;

public static partial class DependencyInjection
{
    public static IServiceCollection AddClickHousePersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var tempSp = services.BuildServiceProvider();
        
        LogStartingRegistration(logger);

        services.AddOptions<ClickHouseOptions>()
            .Bind(configuration.GetSection(ClickHouseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        LogOptionsConfigured(logger, nameof(ClickHouseOptions), ClickHouseOptions.SectionName);

        services.AddScoped<IClickHouseConnectionProvider, ClickHouseConnectionProvider>();
        LogConnectionProviderRegistered(logger, nameof(IClickHouseConnectionProvider), nameof(ClickHouseConnectionProvider), "Scoped");

        LogRegistrationCompleted(logger);
        return services;
    }
}
