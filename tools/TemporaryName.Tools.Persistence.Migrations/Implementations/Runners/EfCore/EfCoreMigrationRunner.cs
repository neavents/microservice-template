using Microsoft.Extensions.Logging;
using TemporaryName.Tools.Persistence.Migrations.Abstractions;

namespace TemporaryName.Tools.Persistence.Migrations.Implementations.Runners.EfCore;

public class EfCoreMigrationRunner : IMigrationRunner
{
    private readonly ILogger<EfCoreMigrationRunner> _logger;
    private readonly ProcessRunner _processRunner;

    // Relative path from this tool's execution directory to the solution root. Adjust if needed.
    private const string DefaultSolutionRootRelativePath = "../../../";


    public EfCoreMigrationRunner(ILogger<EfCoreMigrationRunner> logger, ProcessRunner processRunner)
    {
        _logger = logger;
        _processRunner = processRunner;
    }

    public async Task AddMigrationAsync(string migrationName, string? connectionString, string projectPath, string startupProjectPath)
    {
        _logger.LogInformation("EF Core Runner: Adding migration '{MigrationName}'.", migrationName);

        // Basic validation
        ArgumentException.ThrowIfNullOrWhiteSpace(migrationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(startupProjectPath);


        // Construct the dotnet ef command arguments
        // Connection string is generally NOT needed for 'add', but can be passed if specific design-time services require it.
        // Let's omit it for 'add' unless proven necessary.
        string arguments = $"ef migrations add {EscapeArgument(migrationName)} --project \"{projectPath}\" --startup-project \"{startupProjectPath}\" --verbose";

        // Determine working directory (usually the solution root or where 'dotnet' commands work)
        string workingDirectory = GetWorkingDirectory();

        bool success = await _processRunner.RunProcessAsync("dotnet", arguments, workingDirectory);

        if (!success)
        {
            throw new InvalidOperationException($"Failed to add EF Core migration '{migrationName}'. Check logs for details.");
        }
        _logger.LogInformation("EF Core migration '{MigrationName}' added successfully (pending 'apply').", migrationName);
    }


    public async Task RemoveMigrationAsync(string? connectionString, string projectPath, string startupProjectPath)
    {
        _logger.LogInformation("EF Core Runner: Removing last migration.");

        // Basic validation
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(startupProjectPath);

        // Connection string is generally NOT needed for 'remove'.
        string arguments = $"ef migrations remove --project \"{projectPath}\" --startup-project \"{startupProjectPath}\" --force --verbose"; // Use --force to avoid prompt

        string workingDirectory = GetWorkingDirectory();
        bool success = await _processRunner.RunProcessAsync("dotnet", arguments, workingDirectory);

        if (!success)
        {
            throw new InvalidOperationException("Failed to remove the last EF Core migration. Check logs for details.");
        }
        _logger.LogInformation("Last EF Core migration removed successfully.");
    }

    public async Task ApplyMigrationsAsync(string connectionString, string projectPath, string startupProjectPath)
    {
        _logger.LogInformation("EF Core Runner: Applying migrations.");

        // Basic validation
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString); // Required for applying
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(startupProjectPath);


        // Connection string IS needed for 'database update'. Pass it.
        // NOTE: Passing connection string directly might override DbContext configuration in code.
        // Ensure your IDesignTimeDbContextFactory uses args or this connection string appropriately if needed.
        // Often, just having the correct config in appsettings.json of the startup project is enough.
        // Let's try *without* passing it explicitly first, assuming the factory/startup config handles it.
        // If that fails, add: --connection \"{connectionString}\"

        string arguments = $"ef database update --project \"{projectPath}\" --startup-project \"{startupProjectPath}\" --verbose";
        // Optionally add: --connection \"{EscapeArgument(connectionString)}\"

        string workingDirectory = GetWorkingDirectory();
        bool success = await _processRunner.RunProcessAsync("dotnet", arguments, workingDirectory);

        if (!success)
        {
            throw new InvalidOperationException("Failed to apply EF Core migrations. Check logs and connection string.");
        }
        _logger.LogInformation("EF Core migrations applied successfully.");
    }

    // Helper to determine the working directory. Assumes the tool runs from its build output path.
    private string GetWorkingDirectory()
    {
        // Assumes the Migrations Tool project is in a 'tools/' directory sibling to 'src/'
        string basePath = AppContext.BaseDirectory;
        DirectoryInfo? currentDir = new(basePath);

        // Look for a solution file (.sln) or a recognizable root marker (e.g., 'src' directory)
        // Go up a few levels, max 5 to avoid infinite loops in weird structures
        int maxLevels = 5;
        for (int i = 0; i < maxLevels && currentDir != null; i++)
        {
            if (Directory.Exists(Path.Combine(currentDir.FullName, "src")) || currentDir.GetFiles("*.sln").Length > 0)
            {
                _logger.LogDebug("Determined working directory: {WorkingDirectory}", currentDir.FullName);
                return currentDir.FullName;
            }
            currentDir = currentDir.Parent;
        }

        _logger.LogWarning("Could not reliably determine solution root directory. Using application base directory: {BaseDirectory}. 'dotnet ef' commands might fail if not run from the correct root.", basePath);
        return basePath; // Fallback
    }

     // Simple argument escaping (adjust if needed for complex cases)
    private static string EscapeArgument(string arg)
    {
        // Basic escaping for quotes, adjust if necessary for more complex shell interactions
        return arg.Replace("\"", "\\\"");
    }
}