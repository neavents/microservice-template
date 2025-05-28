using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Sql.PostgreSQL;
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PostgreSqlApplicationDbContext>
{
    public PostgreSqlApplicationDbContext CreateDbContext(string[] args)
    {
        // Used by EF Core tools (dotnet ef migrations add, dotnet ef database update)
        // Typically reads connection string from appsettings.Development.json or environment variables for design-time.
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // Adjust path if necessary, e.g., relative to solution
            .AddJsonFile("appsettings.Development.json", optional: true) // Ensure this file exists or use another source
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<PostgreSqlApplicationDbContext>();
        string connectionString = configuration.GetConnectionString("PostgreSqlDefaultConnection") // Define this in your settings
            ?? throw new InvalidOperationException("PostgreSqlDefaultConnection string not found for design-time.");

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(DesignTimeDbContextFactory).Assembly.FullName);
            // Add any other Npgsql specific options here if needed for design time
        });

        return new PostgreSqlApplicationDbContext(optionsBuilder.Options);
    }
}
