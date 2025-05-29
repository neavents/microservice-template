using System;
using Autofac;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Olap.ClickHouseDb;

public class ClickHouseDbModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Autofac-specific registrations for Clickhouse if any.
    }
}
