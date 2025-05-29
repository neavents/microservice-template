using System;
using Autofac;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j;

public class Neo4jModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Autofac-specific registrations for Neo4j if any.
    }
}
