using System;
using Neo4j.Driver;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j.Abstractions;

public interface INeo4jClientProvider : IAsyncDisposable
{
    IDriver Driver { get; }
    Task<IAsyncSession> GetSessionAsync(AccessMode accessMode = AccessMode.Write, string? database = null);
}
