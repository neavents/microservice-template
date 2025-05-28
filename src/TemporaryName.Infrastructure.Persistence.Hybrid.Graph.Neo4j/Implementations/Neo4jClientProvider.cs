using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j.Abstractions;
using TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j.Settings;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j.Implementations;

public sealed partial class Neo4jClientProvider : INeo4jClientProvider 
{
    private readonly IDriver _driver;
    private readonly Neo4jOptions _options;
    private readonly ILogger<Neo4jClientProvider> _logger; 

    public Neo4jClientProvider(IOptions<Neo4jOptions> optionsAccessor, ILogger<Neo4jClientProvider> logger) 
    {
        ArgumentNullException.ThrowIfNull(optionsAccessor);
        ArgumentNullException.ThrowIfNull(optionsAccessor.Value);
        ArgumentNullException.ThrowIfNull(logger); 

        _options = optionsAccessor.Value;
        _logger = logger; 

        try
        {
            LogAttemptingToCreateDriver(_logger, _options.Uri);
            ConfigBuilder configBuilder = new();
            if (_options.UseEncryption)
            {
                configBuilder.WithEncryptionLevel(EncryptionLevel.Encrypted);
                // For Aura or cloud instances, often you don't need to specify trust explicitly
                // unless you have custom certificates. For self-hosted with self-signed certs:
                // configBuilder.WithTrustManager(TrustManager.CreateChainTrust(pathToTrustedCert));
                // Or for no trust validation (dev only!):
                // configBuilder.WithTrustManager(TrustManager.CreateInsecure());
                LogEncryptionConfiguration(_logger, "Enabled");
            }
            else
            {
                LogEncryptionConfiguration(_logger, "Disabled");
            }

            configBuilder.WithMaxConnectionPoolSize(_options.MaxConnectionPoolSize);
            configBuilder.WithConnectionTimeout(_options.ConnectionTimeout);
            configBuilder.WithSessionAcquisitionTimeout(_options.SessionAcquisitionTimeout);
            // Add other config settings from _options if needed

            IAuthToken authToken = string.IsNullOrWhiteSpace(_options.Username) || string.IsNullOrWhiteSpace(_options.Password)
                ? AuthTokens.None
                : AuthTokens.Basic(_options.Username, _options.Password, null); // Realm is usually null

            _driver = GraphDatabase.Driver(_options.Uri, authToken, configBuilder.Build());
            LogDriverCreatedSuccessfully(_logger, _options.Uri); // Logging
        }
        catch (Exception ex)
        {
            LogDriverCreationFailure(_logger, _options.Uri, ex.Message, ex); // Logging
            // Wrap and rethrow or handle as appropriate for your application startup
            throw new InvalidOperationException($"Failed to initialize Neo4j driver for URI '{_options.Uri}'. See inner exception.", ex);
        }
    }

    public IDriver Driver => _driver;

    public Task<IAsyncSession> GetSessionAsync(AccessMode accessMode = AccessMode.Write, string? database = null)
    {
        string targetDatabase = database ?? _options.Database ?? "neo4j"; // Default to "neo4j"
        LogCreatingSession(_logger, accessMode.ToString(), targetDatabase); // Logging
        return Task.FromResult(_driver.AsyncSession(o => o.WithDatabase(targetDatabase).WithDefaultAccessMode(accessMode)));
    }

    public async ValueTask DisposeAsync()
    {
        if (_driver != null)
        {
            LogDisposingDriver(_logger, _options.Uri); // Logging
            await _driver.DisposeAsync().ConfigureAwait(false);
            LogDriverDisposed(_logger, _options.Uri); // Logging
        }
        GC.SuppressFinalize(this);
    }
}