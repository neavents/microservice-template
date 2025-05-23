using System;
using Autofac;

namespace TemporaryName.Infrastructure.Caching.Memcached;

public class MemcachedCacheModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        // Minimal registrations here. Primary setup is in DependencyInjection.cs
        // Example: If you had an interceptor specific to Memcached caching behavior
        // builder.RegisterType<MyMemcachedSpecificInterceptor>().Keyed<IInterceptor>(InterceptorKeys.MemcachedFeature);
    }
}