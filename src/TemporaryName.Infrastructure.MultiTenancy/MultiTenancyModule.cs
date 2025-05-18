using System;
using Autofac;
using Autofac.Core.Resolving.Pipeline;
using Castle.DynamicProxy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Implementations;
using TemporaryName.Infrastructure.MultiTenancy.Implementations.Interceptors;
using TemporaryName.Infrastructure.MultiTenancy.Implementations.Providers;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy;

/// <summary>
/// Autofac module for providing advanced MultiTenancy features, primarily caching for ITenantStore.
/// Assumes that core multi-tenancy services (including a base ITenantStore registration)
/// have already been registered with IServiceCollection (e.g., via the AddMultiTenancy extension method).
/// This module enhances the existing ITenantStore registration with caching if enabled.
/// </summary>
public class MultiTenancyModule : Module
{
    public MultiTenancyModule() { }

    protected override void Load(ContainerBuilder builder)
    {
        var diagnosticLogger = GetDiagnosticsLogger(builder);
        diagnosticLogger.LogInformation("[Autofac Module] MultiTenancyModule loading. Will attempt to enhance ITenantStore with caching if enabled.");

        builder.RegisterType<CachingTenantStoreInterceptor>()
               .AsSelf() // Register as self to be resolved by type by the pipeline
               .IfNotRegistered(typeof(CachingTenantStoreInterceptor))
               .InstancePerLifetimeScope(); // Interceptor can be scoped
        diagnosticLogger.LogDebug("[Autofac Module] CachingTenantStoreInterceptor registered.");

        builder.RegisterDecorator<ITenantStore>(
            (c, _, inner) =>
            {
                var options = c.Resolve<MultiTenancyOptions>();
                if (!options.Store.Cache.Enabled)
                {
                    diagnosticLogger.LogDebug("[Autofac Module] Tenant store caching is disabled. Returning undecorated ITenantStore.");
                    return inner; // Return the original undecorated store
                }

                diagnosticLogger.LogDebug("[Autofac Module] Tenant store caching is enabled. Decorating ITenantStore with CachingTenantStoreInterceptor.");
                var interceptor = c.Resolve<CachingTenantStoreInterceptor>();
                var proxyGenerator = new ProxyGenerator();
                return proxyGenerator.CreateInterfaceProxyWithTargetInterface<ITenantStore>(inner, interceptor);
            });

        diagnosticLogger.LogInformation("[Autofac Module] MultiTenancyModule loaded. ITenantStore is configured for potential interception.");
    }

    private ILogger GetDiagnosticsLogger(ContainerBuilder builder)
    {
        ILogger logger = null!;
        builder.RegisterBuildCallback(cr =>
        {

        });

        using var tempLoggerFactory = LoggerFactory.Create(lb => lb.AddConsole().SetMinimumLevel(LogLevel.Debug));
        return tempLoggerFactory.CreateLogger<MultiTenancyModule>();
    }
}