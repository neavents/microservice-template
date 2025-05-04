using System;
using Autofac;
using Castle.DynamicProxy;

namespace TemporaryName.Common.Autofac;

public static class ContainerBuilderExtensions
{
    public static ContainerBuilder RegisterInterceptor<T>(this ContainerBuilder builder, string interceptorKey) where T : class, IInterceptor{
        if (string.IsNullOrWhiteSpace(interceptorKey)) {
            throw new ArgumentNullException(nameof(interceptorKey), "An interceptor key must be provided.");
        }

        builder.RegisterType<T>()
            .Named<IInterceptor>(interceptorKey)
            .InstancePerLifetimeScope()
            .IfNotRegistered(typeof(T)); 

        return builder;
    }

}
