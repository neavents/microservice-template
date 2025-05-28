using System;
using Autofac;

namespace TemporaryName.Infrastructure.Caching.Redis;

public class RedisCacheModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        //Register your services
    }
}
