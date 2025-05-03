using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration; // Needed to potentially read config

namespace TemporaryName.Tools.Persistence.Migrations.DesignTime;

// TODO: Replace 'DesignTimePlaceholderDbContext' with your ACTUAL DbContext class name
// from the PostgreSQL project. This class MUST exist in that project.
// using YourActualDbContextNamespace; // Import the namespace of your real DbContext

// This factory tells `dotnet ef` how to create your DbContext instance at design time.
public class DesignTimePlaceholderDbContextFactory // : IDesignTimeDbContextFactory<YourActualDbContext> // <-- Change this
{
    // Make sure this method returns YOUR ACTUAL DbContext type
    // public YourActualDbContext CreateDbContext(string[] args) // <-- Change return type
    public DesignTimePlaceholderDbContext CreateDbContext(string[] args) // <-- Keep this if using the placeholder below
    {
        // Build configuration to potentially read connection strings, same way the app does
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // EF tools run from project dir
             // Adjust path if appsettings is elsewhere relative to the *Persistence* project
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.Development.json", optional: true) // Or determine environment appropriately
            .AddEnvironmentVariables()
            .AddCommandLine(args) // Pass args for potential overrides
            .Build();

        DbContextOptionsBuilder<DesignTimePlaceholderDbContext> optionsBuilder = new(); // <-- Change this to your actual DbContext
        // DbContextOptionsBuilder<YourActualDbContext> optionsBuilder = new(); // <-- Like this


        // Get connection string (e.g., from config). EF Tools prioritize appsettings/user secrets in the startup project.
        // You might not need to explicitly read it here if your startup project's config is sufficient.
        string connectionString = configuration.GetConnectionString("postgresql") // Match key in appsettings
                                  ?? "Host=localhost;Database=DesignTimePlaceholder;Username=user;Password=pass"; // Fallback ONLY for design time

        // Configure the DbContextOptionsBuilder for YOUR DbContext
        optionsBuilder.UseNpgsql(connectionString, options =>
            options.MigrationsAssembly(typeof(DesignTimePlaceholderDbContextFactory).Assembly.GetName().Name)); // Assumes migrations are in the *persistence* project
            // If migrations are in the Persistence project, use its assembly name:
            // options.MigrationsAssembly("TemporaryName.Infrastructure.Persistence.Hybrid.Sql.PostgreSQL"));


        _ = new DesignTimePlaceholderDbContext(optionsBuilder.Options);
        // return new YourActualDbContext(optionsBuilder.Options); // <-- Change this
        return new DesignTimePlaceholderDbContext(optionsBuilder.Options); // <-- Keep placeholder if using below
    }
}

// Placeholder DbContext class itself - REMOVE THIS ONCE YOU USE YOUR REAL ONE ABOVE
// This is only here so the IDesignTimeDbContextFactory compiles initially.
public class DesignTimePlaceholderDbContext(DbContextOptions<DesignTimePlaceholderDbContext> options) : DbContext(options)
{
}