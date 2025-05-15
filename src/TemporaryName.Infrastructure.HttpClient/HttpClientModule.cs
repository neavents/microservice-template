using System;
using Autofac;

namespace TemporaryName.Infrastructure.HttpClient;

public class HttpClientModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        //Register your services
    }
}
