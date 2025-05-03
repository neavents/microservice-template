using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Spectre.Console.Cli;
// Removed Spectre.Console.Extensions.DependencyInjection using
using System.Diagnostics;
using System.Reflection;
using TemporaryName.Tools.Persistence.Migrations.Abstractions;
using TemporaryName.Tools.Persistence.Migrations.Commands;
using TemporaryName.Tools.Persistence.Migrations.Configuration;
using TemporaryName.Tools.Persistence.Migrations.Implementations;
using TemporaryName.Tools.Persistence.Migrations.Implementations.Runners.EfCore;

namespace TemporaryName.Tools.Persistence.Migrations;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        if (!await IsDotnetEfInstalledAsync())
        {
            Log.Fatal("'.NET EF Core tools' not found or 'dotnet ef --version' failed. Install globally: dotnet tool install --global dotnet-ef");
            return -1;
        }

        // --- Step 1: Configure Services ---
        // Create a service collection and configure basic services needed *before* host build
        // (like configuration itself, potentially basic logging setup if needed early).
         ServiceCollection services = new();
         ConfigureInitialServices(services, args); // Configure config, maybe logging essentials

         // --- Step 2: Build Configuration (needed for full service config) ---
         ServiceProvider tempProvider = services.BuildServiceProvider();
         IConfiguration configuration = tempProvider.GetRequiredService<IConfiguration>();


        // --- Step 3: Configure Core Application Services ---
        // Now configure all the main application services using the built configuration
        ConfigureApplicationServices(services, configuration);


        // --- Step 4: Set up Spectre.Console.Cli with the DI container ---
        // Use our custom TypeRegistrar that hooks into the ServiceCollection
        SpectreTypeRegistrar registrar = new(services); // Pass the service collection
        CommandApp app = new(registrar); // Spectre will call Build() on the registrar later


        app.Configure(config =>
        {
            config.SetApplicationName("migrations-tool");
            config.ValidateExamples();

            // Commands are resolved via DI later by the SpectreTypeResolver
            config.AddCommand<AddMigrationCommand>("add")
                  .WithDescription("Add a new database migration.")
                  .WithExample(["add", "InitialCreate", "-t", "postgresql", "-p", "../Path/To/Persistence"]);

            config.AddCommand<RemoveMigrationCommand>("remove")
                  .WithDescription("Remove the last database migration.")
                  .WithExample(["remove", "-t", "postgresql", "-p", "../Path/To/Persistence"]);

            config.AddCommand<ApplyMigrationsCommand>("apply")
                  .WithDescription("Apply migrations to the database.")
                  .WithExample(["apply", "-t", "postgresql", "-c", "YourConnectionString"]);

            #if DEBUG
            config.PropagateExceptions();
            #endif
        });


        Log.Information("Starting Migrations Tool");
        int exitCode = -1;
        try
        {
            // --- Step 5: Run the Command App ---
            // Spectre's RunAsync will trigger the registrar's Build() method,
            // which builds the final ServiceProvider from our configured ServiceCollection.
            // Commands and their dependencies will be resolved from this provider.
            exitCode = await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Migrations Tool terminated unexpectedly.");
            exitCode = 1;
        }
        finally
        {
            Log.Information("Stopping Migrations Tool. Exit Code: {ExitCode}", exitCode);
            await Log.CloseAndFlushAsync();

            // Dispose the temporary provider if it holds disposable services, though unlikely needed here.
            // The main provider built by SpectreTypeRegistrar will be disposed if SpectreTypeResolver.Dispose() is implemented to do so.
            await tempProvider.DisposeAsync();
        }
        return exitCode;
    }

     // Configure services needed early, primarily Configuration
    private static void ConfigureInitialServices(IServiceCollection services, string[] args)
    {
         IConfiguration configuration = new ConfigurationBuilder()
             .SetBasePath(AppContext.BaseDirectory)
             .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
             // Determine EnvironmentName correctly if needed for appsettings.{Env}.json
             // .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
             .AddEnvironmentVariables()
             .AddCommandLine(args)
             .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Add logging here if needed before full Host build (e.g., for config issues)
        // services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
    }


    // Configure the main application services
    private static void ConfigureApplicationServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configure Serilog using the final configuration
        services.AddLogging(loggingBuilder =>
             loggingBuilder.AddSerilog(
                 new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration) // Read from IConfiguration
                    .Enrich.FromLogContext()
                    .CreateLogger(),
                dispose: true)); // Dispose logger with provider


        // Register application services
        services.AddSingleton<ConnectionStringResolver>();
        services.AddSingleton<ProcessRunner>();

        services.AddSingleton<IMigrationServiceFactory, MigrationServiceFactory>();
        services.AddKeyedSingleton<IMigrationRunner, EfCoreMigrationRunner>("postgresql");
        // services.AddKeyedSingleton<IMigrationRunner, CassandraMigrationRunner>("cassandra");

        // NOTE: No need to build the full IHost anymore unless background services are needed.
        // For a simple CLI tool, configuring services directly and letting Spectre build the provider is sufficient.
    }

    // Removed CreateHostBuilder and related Host logic as it's not strictly needed
    // for this CLI structure anymore. DI is handled via ServiceCollection directly.


    private static async Task<bool> IsDotnetEfInstalledAsync()
    {
        try
        {
            // Use CancellationToken based timeout for process check
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            ProcessStartInfo psi = new("dotnet", "ef --version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using Process process = new() { StartInfo = psi };
            process.Start();
            // Correct way to wait with timeout using CancellationToken
             await process.WaitForExitAsync(cts.Token);
            return process.ExitCode == 0;
        }
         catch (OperationCanceledException) // Catches timeout specifically
         {
             Log.Warning("Checking for dotnet ef tool timed out.");
             return false;
         }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to check for dotnet ef tool presence.");
            return false;
        }
    }
}