using System;
using System.Data.Common;
using ClickHouse.Client.ADO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.Persistence.Hybrid.Olap.ClickHouseDb.Abstractions;
using TemporaryName.Infrastructure.Persistence.Hybrid.Olap.ClickHouseDb.Settings;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Olap.ClickHouseDb.Implementations;

public sealed partial class ClickHouseConnectionProvider : IClickHouseConnectionProvider
{
    private readonly ClickHouseOptions _options;
    private readonly ILogger<ClickHouseConnectionProvider> _logger;

    public ClickHouseConnectionProvider(IOptions<ClickHouseOptions> optionsAccessor, ILogger<ClickHouseConnectionProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(optionsAccessor);
        ArgumentNullException.ThrowIfNull(optionsAccessor.Value);
        ArgumentNullException.ThrowIfNull(logger);

        _options = optionsAccessor.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            LogMissingConnectionString(_logger);
            throw new InvalidOperationException("ClickHouse connection string is not configured in ClickHouseOptions.");
        }

        // Validate SSL configuration intent vs. connection string
        bool connectionStringIndicatesSsl = _options.ConnectionString.Contains("Ssl=True", StringComparison.OrdinalIgnoreCase) ||
                                           _options.ConnectionString.Contains("UseSsl=true", StringComparison.OrdinalIgnoreCase) || // Some drivers use this
                                           _options.ConnectionString.Contains("Protocol=Https", StringComparison.OrdinalIgnoreCase); // HTTPS implies SSL

        if (_options.UseSsl && !connectionStringIndicatesSsl)
        {
            LogSslOptionMismatchWarning(_logger, _options.ConnectionString);
            // Potentially modify connection string here if UseSsl is true but not in CS, or throw.
            // For now, we'll rely on the CS being correctly formatted.
        } else if (!_options.UseSsl && connectionStringIndicatesSsl)
        {
            LogSslOptionMismatchWarning(_logger, _options.ConnectionString, "Connection string indicates SSL but UseSsl option is false.");
        }


        LogConnectionStringConfigured(_logger, SanitizeConnectionString(_options.ConnectionString), _options.UseSsl);
    }

    public DbConnection CreateConnection()
    {
        LogCreatingConnection(_logger);
        // ClickHouseConnection from ClickHouse.Client library
        var connection = new ClickHouseConnection(_options.ConnectionString);
        return connection;
    }

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        DbConnection connection = CreateConnection();
        try
        {
            LogOpeningConnection(_logger);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            LogConnectionOpenedSuccessfully(_logger);
            return connection;
        }
        catch (Exception ex)
        {
            LogConnectionOpenFailure(_logger, ex.Message, ex);
            // connection.Dispose() is called by DbConnection's Dispose if OpenAsync throws,
            // but explicit DisposeAsync can be good practice in catch blocks.
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public void Dispose()
    {
        // This provider doesn't hold a persistent connection object itself.
        // Connections created by it are meant to be disposed by their consumer.
        LogDisposingProvider(_logger);
        GC.SuppressFinalize(this);
    }

    private static string SanitizeConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return "EMPTY_OR_NULL";
        // A more robust regex might be needed if connection string format varies widely
        return System.Text.RegularExpressions.Regex.Replace(connectionString, @"Password=[^;]*", "Password=*****", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}