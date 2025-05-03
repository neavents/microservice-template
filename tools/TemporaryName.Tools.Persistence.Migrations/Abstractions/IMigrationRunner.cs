namespace TemporaryName.Tools.Persistence.Migrations.Abstractions;

/// <summary>
/// Defines operations for running migrations for a specific database type.
/// </summary>
public interface IMigrationRunner
{
    /// <summary>
    /// Adds a new migration.
    /// </summary>
    /// <param name="migrationName">The name for the migration.</param>
    /// <param name="connectionString">The connection string (might not be used by all runners, e.g., EF Core add).</param>
    /// <param name="projectPath">Path to the persistence project containing models/DbContext.</param>
    /// <param name="startupProjectPath">Path to the startup project (this tool's project).</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task AddMigrationAsync(string migrationName, string? connectionString, string projectPath, string startupProjectPath);

    /// <summary>
    /// Removes the last applied migration.
    /// </summary>
    /// <param name="connectionString">The connection string (might not be used by all runners).</param>
    /// <param name="projectPath">Path to the persistence project.</param>
    /// <param name="startupProjectPath">Path to the startup project.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task RemoveMigrationAsync(string? connectionString, string projectPath, string startupProjectPath);

    /// <summary>
    /// Applies all pending migrations to the database.
    /// </summary>
    /// <param name="connectionString">The connection string to the target database.</param>
    /// <param name="projectPath">Path to the persistence project.</param>
    /// <param name="startupProjectPath">Path to the startup project.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task ApplyMigrationsAsync(string connectionString, string projectPath, string startupProjectPath);
}