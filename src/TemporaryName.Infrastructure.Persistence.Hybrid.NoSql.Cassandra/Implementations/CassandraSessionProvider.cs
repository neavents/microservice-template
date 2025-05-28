using System;
using Cassandra;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra.Abstractions;
using TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra.Settings;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra.Implementations;

public sealed partial class CassandraSessionProvider : ICassandraSessionProvider
{
    private readonly ICluster _cluster;
    private readonly CassandraOptions _options;
    private readonly ILogger<CassandraSessionProvider> _logger; 

    public CassandraSessionProvider(IOptions<CassandraOptions> optionsAccessor, ILogger<CassandraSessionProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(optionsAccessor);
        ArgumentNullException.ThrowIfNull(optionsAccessor.Value);
        ArgumentNullException.ThrowIfNull(logger);

        _options = optionsAccessor.Value;
        _logger = logger;

        LogAttemptingToCreateCluster(_logger, _options.ContactPoints);
        try
        {
            var builder = Cluster.ConnectAsync()
                .AddContactPoints(_options.ContactPoints.Split(',').Select(cp => cp.Trim()).ToArray())
                .WithPort(_options.Port)
                .WithQueryTimeout((int)_options.QueryTimeout.TotalMilliseconds)
                .WithSocketOptions(new SocketOptions().SetConnectTimeoutMillis((int)_options.ConnectTimeout.TotalMilliseconds));

            if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
            {
                builder.WithCredentials(_options.Username, _options.Password);
                 LogCredentialsConfigured(_logger, _options.Username);
            }

            if (!string.IsNullOrWhiteSpace(_options.LocalDatacenter))
            {
                builder.WithLoadBalancingPolicy(new TokenAwarePolicy(new DCAwareRoundRobinPolicy(_options.LocalDatacenter)));
                LogLoadBalancingPolicyConfigured(_logger, _options.LocalDatacenter);
            }

            if (_options.UseSsl)
            {
                builder.WithSSL();
                LogSslConfiguration(_logger, "Enabled (further SSLOptions configuration might be needed)");
            } else
            {
                LogSslConfiguration(_logger, "Disabled");
            }
            
            //TODO
            builder.WithPoolingOptions();  

            _cluster = builder.Build();
             LogClusterCreatedSuccessfully(_logger, _options.ContactPoints);
        }
        catch (Exception ex)
        {
             LogClusterCreationFailure(_logger, _options.ContactPoints, ex.Message, ex);
            throw new InvalidOperationException($"Failed to initialize Cassandra cluster for contact points '{_options.ContactPoints}'. See inner exception.", ex);
        }
    }

    public ICluster Cluster => _cluster;

    public async Task<ISession> GetSessionAsync(string? keyspace = null)
    {
        string targetKeyspace = keyspace ?? _options.DefaultKeyspace;
        LogCreatingSession(_logger, targetKeyspace); 
        // ConnectAsync will create a new session if one for this keyspace isn't already cached by the driver,
        // or return an existing one. Sessions are thread-safe.
        return await _cluster.ConnectAsync(targetKeyspace).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_cluster is not null)
        {
            LogDisposingCluster(_logger, _options.ContactPoints);
            _cluster.Dispose();
            LogClusterDisposed(_logger, _options.ContactPoints);
        }
        GC.SuppressFinalize(this);
    }
}
