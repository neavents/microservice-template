using System;
using Cassandra;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra.Abstractions;

public interface ICassandraSessionProvider : IAsyncDisposable
{
    Task<ISession> GetSessionAsync(string? keyspace = null);
    ICluster Cluster { get; }
}
