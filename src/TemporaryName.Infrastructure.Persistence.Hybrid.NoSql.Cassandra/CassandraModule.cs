using System;
using Autofac;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra;

public class CassandraModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Autofac-specific registrations for Cassandra if any.
    }
}
