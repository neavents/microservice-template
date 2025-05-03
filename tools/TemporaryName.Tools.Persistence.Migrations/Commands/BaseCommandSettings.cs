using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace TemporaryName.Tools.Persistence.Migrations.Commands;

public class BaseCommandSettings : CommandSettings
{
    [CommandOption("-t|--db-type")]
    [Description("The target database type (e.g., postgresql, cassandra). Default: postgresql.")]
    [DefaultValue("postgresql")] // Set default value
    public string DbType { get; set; } = "postgresql"; // Initialize property

    [CommandOption("-p|--project")]
    [Description("REQUIRED. Relative path to the project containing persistence models/DbContext (e.g., ../../src/MyPersistenceProject).")]
    public string? ProjectPath { get; set; }
    // Note: Made nullable initially, will validate in command execution. Could make non-nullable and rely on Spectre validation.

    [CommandOption("-c|--connection-string")]
    [Description("Database connection string (overrides environment variables and appsettings.json).")]
    public string? ConnectionString { get; set; }

    // Validate common required settings
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(ProjectPath))
        {
            return ValidationResult.Error("--project path is required.");
        }
         // Basic check if path looks plausible (doesn't guarantee existence yet)
        if (ProjectPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length < 2)
        {
            return ValidationResult.Error("--project path seems too short or invalid. Use relative path from solution root.");
        }

        return ValidationResult.Success();
    }
}