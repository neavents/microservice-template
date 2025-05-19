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
public partial class MultiTenancyModule : Module
{
    ILogger? _logger;
    public MultiTenancyModule() { }

    protected override void Load(ContainerBuilder builder)
    {
        EnsureLoggerInitialized();

        LogModuleState(_logger, nameof(MultiTenancyModule), "loading", $"Will attempt to enhance {nameof(ITenantStore)} with caching if enabled.");

        builder.RegisterType<CachingTenantStoreInterceptor>()
               .AsSelf() // Register as self to be resolved by type by the pipeline
               .IfNotRegistered(typeof(CachingTenantStoreInterceptor))
               .InstancePerLifetimeScope(); // Interceptor can be scoped
        
        LogRegistered(_logger, nameof(CachingTenantStoreInterceptor));

        builder.RegisterDecorator<ITenantStore>(
            (c, _, inner) =>
            {
                EnsureLoggerInitialized();

                var options = c.Resolve<MultiTenancyOptions>();
                if (!options.Store.Cache.Enabled)
                {
                    LogCachingState(_logger, "disabled", $"Returning undecorated {nameof(ITenantStore)}.");

                    return inner;
                }
                
                LogCachingState(_logger, "enabled", $"Decorating {nameof(ITenantStore)} with {nameof(CachingTenantStoreInterceptor)}.");

                var interceptor = c.Resolve<CachingTenantStoreInterceptor>();
                var proxyGenerator = new ProxyGenerator();

                return proxyGenerator.CreateInterfaceProxyWithTargetInterface<ITenantStore>(inner, interceptor);
            });

        LogModuleState(_logger, nameof(MultiTenancyModule), "loaded", $"{nameof(ITenantStore)} is configured for potantial interception");
    }

    private void EnsureLoggerInitialized()
    {
        if (_logger is null)
        {
            using var tempLoggerFactory = LoggerFactory.Create(lb => lb.AddConsole().SetMinimumLevel(LogLevel.Debug));
            _logger = tempLoggerFactory.CreateLogger<MultiTenancyModule>();
        }
    }
    
}