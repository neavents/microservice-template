using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using TemporaryName.Tools.Persistence.Migrations.Abstractions;
using TemporaryName.Tools.Persistence.Migrations.Configuration;

namespace TemporaryName.Tools.Persistence.Migrations.Commands;

public sealed class ApplyMigrationsCommand : AsyncCommand<ApplyMigrationsCommand.Settings>
{
     private readonly ILogger<ApplyMigrationsCommand> _logger;
    private readonly IMigrationServiceFactory _migrationServiceFactory;
    private readonly ConnectionStringResolver _connectionStringResolver;

    // Inherit common settings, no specific arguments needed for 'apply'
     public sealed class Settings : BaseCommandSettings { }

      public ApplyMigrationsCommand(
        ILogger<ApplyMigrationsCommand> logger,
        IMigrationServiceFactory migrationServiceFactory,
        ConnectionStringResolver connectionStringResolver)
    {
        _logger = logger;
        _migrationServiceFactory = migrationServiceFactory;
        _connectionStringResolver = connectionStringResolver;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
         _logger.LogInformation("Executing 'apply' command...");
         _logger.LogDebug("Settings: DbType={DbType}, ProjectPath={ProjectPath}, ConnectionString CLI Override={CS}",
            settings.DbType, settings.ProjectPath, settings.ConnectionString ?? "N/A");

        try
        {
            // Connection string IS required for applying migrations
            string resolvedConnectionString = _connectionStringResolver.ResolveConnectionString(settings.DbType, settings.ConnectionString)
                 ?? throw new InvalidOperationException($"Connection string for database type '{settings.DbType}' could not be resolved. Provide via -c, environment variable, or appsettings.json.");

            IMigrationRunner runner = _migrationServiceFactory.GetRunner(settings.DbType);
             string startupProjectPath = Directory.GetCurrentDirectory();
             string absoluteProjectPath = Path.GetFullPath(settings.ProjectPath!);

            await runner.ApplyMigrationsAsync(resolvedConnectionString, absoluteProjectPath, startupProjectPath);

            _logger.LogInformation("'apply' command completed successfully.");
            return 0; // Success
        }
         catch (NotSupportedException ex)
        {
             _logger.LogError(ex, "Database type '{DbType}' is not supported.", settings.DbType);
             return 1; // Failure
        }
         catch (ArgumentException ex)
        {
             _logger.LogError(ex, "Invalid argument provided.");
             return 1;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to apply migrations.");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "An unexpected error occurred during the 'apply' command.");
            return 1;
        }
    }
}