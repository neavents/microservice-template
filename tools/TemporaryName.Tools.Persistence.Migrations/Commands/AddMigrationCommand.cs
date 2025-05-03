using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using TemporaryName.Tools.Persistence.Migrations.Abstractions;
using TemporaryName.Tools.Persistence.Migrations.Configuration;

namespace TemporaryName.Tools.Persistence.Migrations.Commands;

public sealed class AddMigrationCommand : AsyncCommand<AddMigrationCommand.Settings>
{
    private readonly ILogger<AddMigrationCommand> _logger;
    private readonly IMigrationServiceFactory _migrationServiceFactory;
    private readonly ConnectionStringResolver _connectionStringResolver;


    public sealed class Settings : BaseCommandSettings // Inherit common settings
    {
        [CommandArgument(0, "<NAME>")]
        [Description("The name for the migration (e.g., InitialCreate, AddUserTable).")]
        public string MigrationName { get; set; } = string.Empty; // Required argument

        public override ValidationResult Validate()
        {
            ValidationResult baseResult = base.Validate();
            if (!baseResult.Successful) return baseResult;

            if (string.IsNullOrWhiteSpace(MigrationName))
            {
                return ValidationResult.Error("Migration name argument is required.");
            }
            // Add more name validation if needed (e.g., regex for allowed characters)

            return ValidationResult.Success();
        }
    }

     public AddMigrationCommand(
        ILogger<AddMigrationCommand> logger,
        IMigrationServiceFactory migrationServiceFactory,
        ConnectionStringResolver connectionStringResolver)
    {
        _logger = logger;
        _migrationServiceFactory = migrationServiceFactory;
         _connectionStringResolver = connectionStringResolver;
    }


    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        _logger.LogInformation("Executing 'add' command...");
        _logger.LogDebug("Settings: DbType={DbType}, ProjectPath={ProjectPath}, MigrationName={MigrationName}, ConnectionString CLI Override={CS}",
            settings.DbType, settings.ProjectPath, settings.MigrationName, settings.ConnectionString ?? "N/A");

        try
        {
             // Resolve connection string (might not be needed for 'add' but good practice to resolve early)
            string? resolvedConnectionString = _connectionStringResolver.ResolveConnectionString(settings.DbType, settings.ConnectionString);

            IMigrationRunner runner = _migrationServiceFactory.GetRunner(settings.DbType);

             // Get the path to the startup project (this tool) dynamically
             string startupProjectPath = Directory.GetCurrentDirectory(); // Or more robustly find the .csproj

            // Ensure project path is absolute for the runner
            string absoluteProjectPath = Path.GetFullPath(settings.ProjectPath!); // Not null due to validation

            await runner.AddMigrationAsync(settings.MigrationName, resolvedConnectionString, absoluteProjectPath, startupProjectPath);

            _logger.LogInformation("'add' command completed successfully for migration '{MigrationName}'.", settings.MigrationName);
            return 0; // Success
        }
        catch (NotSupportedException ex)
        {
             _logger.LogError(ex, "Database type '{DbType}' is not supported.", settings.DbType);
             return 1; // Failure
        }
         catch (ArgumentException ex) // Catch validation errors missed or specific arg errors
        {
             _logger.LogError(ex, "Invalid argument provided.");
             return 1;
        }
        catch (InvalidOperationException ex) // Catch failures from the runner
        {
            _logger.LogError(ex, "Failed to add migration '{MigrationName}'.", settings.MigrationName);
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "An unexpected error occurred during the 'add' command.");
            return 1;
        }
    }
}