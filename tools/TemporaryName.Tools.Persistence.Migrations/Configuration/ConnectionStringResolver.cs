using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Tools.Persistence.Migrations.Configuration;

/// <summary>
/// Resolves the database connection string based on a defined precedence.
/// Precedence: Command Line > Environment Variable > appsettings.json (Specific > Default)
/// </summary>
public class ConnectionStringResolver
{
    private readonly IConfiguration _configuration;
     private readonly ILogger<ConnectionStringResolver> _logger;


    public ConnectionStringResolver(IConfiguration configuration, ILogger<ConnectionStringResolver> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string? ResolveConnectionString(string dbType, string? commandLineConnectionString)
    {
        // 1. Command Line Argument (-c or --connection-string)
        if (!string.IsNullOrWhiteSpace(commandLineConnectionString))
        {
             _logger.LogDebug("Using connection string from command line argument for DbType '{DbType}'.", dbType);
            return commandLineConnectionString;
        }

        // 2. Environment Variable (e.g., ConnectionStrings__postgresql=...)
        // ASPNETCORE_ environment variables are loaded by default Host builder
        // We check the standard format loaded into IConfiguration
        string? envVarKeySpecific = $"ConnectionStrings:{dbType}";
        string? envVarValueSpecific = _configuration[envVarKeySpecific];
        if (!string.IsNullOrWhiteSpace(envVarValueSpecific))
        {
             _logger.LogDebug("Using connection string from environment variable '{Key}' for DbType '{DbType}'.", envVarKeySpecific, dbType);
             return envVarValueSpecific;
        }

         // Check for generic fallback env var if specific not found
         string? envVarKeyDefault = "ConnectionStrings:DefaultConnection";
         string? envVarValueDefault = _configuration[envVarKeyDefault];
         if (!string.IsNullOrWhiteSpace(envVarValueDefault))
         {
             _logger.LogDebug("Using connection string from environment variable '{Key}' (fallback) for DbType '{DbType}'.", envVarKeyDefault, dbType);
             return envVarValueDefault;
         }


        // 3. appsettings.json (Specific key, e.g., ConnectionStrings:postgresql)
        string? appSettingsSpecific = _configuration.GetConnectionString(dbType.ToLowerInvariant());
        if (!string.IsNullOrWhiteSpace(appSettingsSpecific))
        {
            _logger.LogDebug("Using connection string from appsettings.json key '{Key}' for DbType '{DbType}'.", dbType.ToLowerInvariant(), dbType);
            return appSettingsSpecific;
        }

        // 4. appsettings.json (Default key, e.g., ConnectionStrings:DefaultConnection)
        string? appSettingsDefault = _configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(appSettingsDefault))
        {
             _logger.LogDebug("Using connection string from appsettings.json key 'DefaultConnection' (fallback) for DbType '{DbType}'.", dbType);
            return appSettingsDefault;
        }

        _logger.LogWarning("Connection string for DbType '{DbType}' could not be resolved from any source (CLI, EnvVars, appsettings).", dbType);
        return null; // Indicate that it wasn't found
    }
}