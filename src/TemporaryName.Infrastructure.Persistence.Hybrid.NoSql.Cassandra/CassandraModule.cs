using System;
using Autofac;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra;

public class CassandraPersistenceModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Autofac-specific registrations for Cassandra if any.
    }
}
