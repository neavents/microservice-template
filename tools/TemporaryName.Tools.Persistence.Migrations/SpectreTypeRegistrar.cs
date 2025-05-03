using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using System;

namespace TemporaryName.Tools.Persistence.Migrations;

/// <summary>
/// Implements Spectre.Console.Cli's ITypeRegistrar using Microsoft.Extensions.DependencyInjection.
/// </summary>
public sealed class SpectreTypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _builder;

    public SpectreTypeRegistrar(IServiceCollection builder)
    {
        _builder = builder;
    }

    public ITypeResolver Build()
    {
        // Build the service provider only once when Spectre asks for the resolver.
        return new SpectreTypeResolver(_builder.BuildServiceProvider());
    }

    public void Register(Type service, Type implementation)
    {
        _builder.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _builder.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> func)
    {
        // Transient might be suitable here, or Singleton depending on expected lifetime
        _builder.AddSingleton(service, (provider) => func());
    }
}

/// <summary>
/// Implements Spectre.Console.Cli's ITypeResolver using an existing IServiceProvider.
/// </summary>
public sealed class SpectreTypeResolver : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _provider;

    public SpectreTypeResolver(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public object? Resolve(Type? type)
    {
        return type == null ? null : _provider.GetService(type);
    }

    public void Dispose()
    {
        // If the provider is owned by this resolver, dispose it.
        // In our case, the Host owns the provider, so we don't dispose it here.
        if (_provider is IDisposable disposable)
        {
             // Uncomment if the ServiceProvider should be disposed here.
             // disposable.Dispose();
        }
    }
}