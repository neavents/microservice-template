using System;
using System.Reflection;
using Autofac;
using Castle.DynamicProxy;
using SharedKernel.Autofac;

using TemporaryName.Common.Autofac;
using TemporaryName.Infrastructure.Interceptors;

namespace TemporaryName.Infrastructure;

public class InfrastructureModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

         builder.RegisterInterceptor<LoggingInterceptor>(InterceptorKeys.Logging)
                .RegisterInterceptor<DeepLoggingInterceptor>(InterceptorKeys.DeepLogging)
                .RegisterInterceptor<AuditingInterceptor>(InterceptorKeys.Auditing)
                .RegisterInterceptor<CachingInterceptor>(InterceptorKeys.Caching);

        IEnumerable<Assembly> assemblies = [
            ThisAssembly,
            typeof().Assembly,
            ];

        IEnumerable<string> namespaces = [
            "TemporaryName.Infrastructure.Services",
            "TemporaryName.Infrastructure.Caching.Redis.Services"
        ];

        builder.RegisterServicesByConvention(assemblies, namespaces);
    }
}
