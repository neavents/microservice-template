using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using TemporaryName.Tools.Persistence.Migrations.Abstractions;
using TemporaryName.Tools.Persistence.Migrations.Configuration;

namespace TemporaryName.Tools.Persistence.Migrations.Commands;

public sealed class RemoveMigrationCommand : AsyncCommand<RemoveMigrationCommand.Settings>
{
     private readonly ILogger<RemoveMigrationCommand> _logger;
    private readonly IMigrationServiceFactory _migrationServiceFactory;
     private readonly ConnectionStringResolver _connectionStringResolver;


    // Inherit common settings, no specific arguments needed for 'remove'
    public sealed class Settings : BaseCommandSettings { }

    public RemoveMigrationCommand(
        ILogger<RemoveMigrationCommand> logger,
        IMigrationServiceFactory migrationServiceFactory,
        ConnectionStringResolver connectionStringResolver)
    {
        _logger = logger;
        _migrationServiceFactory = migrationServiceFactory;
        _connectionStringResolver = connectionStringResolver;
    }


    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        _logger.LogInformation("Executing 'remove' command...");
         _logger.LogDebug("Settings: DbType={DbType}, ProjectPath={ProjectPath}, ConnectionString CLI Override={CS}",
            settings.DbType, settings.ProjectPath, settings.ConnectionString ?? "N/A");


        try
        {
            string? resolvedConnectionString = _connectionStringResolver.ResolveConnectionString(settings.DbType, settings.ConnectionString);
            IMigrationRunner runner = _migrationServiceFactory.GetRunner(settings.DbType);
            string startupProjectPath = Directory.GetCurrentDirectory();
            string absoluteProjectPath = Path.GetFullPath(settings.ProjectPath!);

            await runner.RemoveMigrationAsync(resolvedConnectionString, absoluteProjectPath, startupProjectPath);

            _logger.LogInformation("'remove' command completed successfully.");
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
            _logger.LogError(ex, "Failed to remove the last migration.");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "An unexpected error occurred during the 'remove' command.");
            return 1;
        }
    }
}