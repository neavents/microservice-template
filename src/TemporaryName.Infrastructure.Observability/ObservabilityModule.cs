using System;
using Autofac;

namespace TemporaryName.Infrastructure.Observability;

public class ObservabilityModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        //Register your services
    }
}
