using System;
using Autofac;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Sql.PostgreSQL;

public class PostgreSqlApplicationDataModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        //Register your services
    }
}
