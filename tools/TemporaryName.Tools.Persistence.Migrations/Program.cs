using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Spectre.Console.Cli;
using System.Diagnostics;
using TemporaryName.Infrastructure;
using TemporaryName.Infrastructure.Hosting.Extensions;
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
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

        try
        {
            if (!await IsDotnetEfInstalledAsync())
            {
                Log.Fatal("'.NET EF Core tools' not found or 'dotnet ef --version' failed. Install globally: dotnet tool install --global dotnet-ef");
                return -1;
            }

            IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureStandardAppConfiguration(args);

            hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());

            hostBuilder.ConfigureContainer<ContainerBuilder>(autofacBuilder =>
            {
                autofacBuilder.RegisterModule<InfrastructureModule>();
            });

            IConfiguration configuration = hostBuilder.Build().Configuration;

            ServiceCollection services = new();
            services.AddSingleton(configuration);
            services.AddLogging(lb => lb.AddSerilog(Log.Logger, dispose: true));
            ConfigureApplicationServices(services, configuration);

            SpectreTypeRegistrar registrar = new(services);
            CommandApp app = new(registrar);
            ConfigureSpectreCommands(app);

            Log.Information("Starting Migrations Tool (using Standard Config)...");
            int exitCode = await app.RunAsync(args);
            Log.Information("Stopping Migrations Tool. Exit Code: {ExitCode}", exitCode);
            return exitCode;
        }
        catch (Exception ex) when (ex is not HostAbortedException)
        {
            Log.Fatal(ex, "Migrations Tool terminated unexpectedly.");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static void ConfigureApplicationServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ConnectionStringResolver>();
        services.AddSingleton<ProcessRunner>();
        services.AddSingleton<IMigrationServiceFactory, MigrationServiceFactory>();
        services.AddKeyedSingleton<IMigrationRunner, EfCoreMigrationRunner>("postgresql");
    }

    private static void ConfigureSpectreCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.SetApplicationName("migrations-tool");
            config.ValidateExamples();
            config.AddCommand<AddMigrationCommand>("add");
            config.AddCommand<RemoveMigrationCommand>("remove");
            config.AddCommand<ApplyMigrationsCommand>("apply");
#if DEBUG
            config.PropagateExceptions();
#endif
        });
    }

    private static async Task<bool> IsDotnetEfInstalledAsync()
    {
        try
        {
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
            await process.WaitForExitAsync(cts.Token);
            return process.ExitCode == 0;
        }
        catch (OperationCanceledException)
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