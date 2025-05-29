
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using TemporaryName.Infrastructure.Outbox.EFCore;
using TemporaryName.Infrastructure.Persistence.Common.EFCore;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Sql.PostgreSQL;

public static class DependencyInjection
{
    public static IServiceCollection AddPostgreSqlPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string? connectionString = configuration.GetConnectionString("PostgreSqlDefaultConnection");
                    var tempSp = services.BuildServiceProvider();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger?.LogCritical("PostgreSQL connection string 'PostgreSqlPrimaryDataConnection' not found or is empty.");
            throw new InvalidOperationException("PostgreSQL connection string 'PostgreSqlDefaultConnection' not found in configuration.");
        }

        services.AddDbContext<PostgreSqlApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(PostgreSqlApplicationDbContext).Assembly.FullName);
                // Add any runtime Npgsql specific options here
                // e.g., npgsqlOptions.EnableRetryOnFailure( ... );
            });

            // IMPORTANT: Add the Outbox SaveChanges Interceptor
            // This resolves the interceptor (registered by Outbox.EFCore's DI) and adds it to this DbContext.
            options.AddOutboxSaveChangesInterceptor(serviceProvider);

            // options.UseLazyLoadingProxies(); // If using lazy loading
            // options.EnableSensitiveDataLogging(); // For development only
        });

        // Register the concrete DbContext also as the base type if other services depend on ApplicationDbContextBase.
        // This allows services to depend on the abstract ApplicationDbContextBase while getting the configured
        // provider-specific instance at runtime.
        //services.AddScoped<ApplicationDbContextBase>(provider => provider.GetRequiredService<PostgreSqlApplicationDbContext>());

        // Add any provider-specific repositories or services here
        // services.AddScoped<IOrderRepository, PostgreSqlOrderRepository>();
        logger?.LogInformation("PostgreSQL Data Persistence registered for DbContext: PostgreSqlApplicationDbContext (includes Outbox).");
        return services;
    }
}
