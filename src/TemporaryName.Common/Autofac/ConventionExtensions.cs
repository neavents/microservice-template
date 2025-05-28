using System;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Extras.DynamicProxy;
using SharedKernel.Autofac;

namespace TemporaryName.Common.Autofac;

public static class ConventionExtensions
{
    /// <summary>
    /// Registers types from specified assemblies based on common conventions.
    /// Filters by namespace, maps to I[ClassName] interface, optionally registers AsSelf,
    /// sets lifetime, and enables interface interception for attribute-based AOP.
    /// </summary>
    /// <param name="builder">The ContainerBuilder instance.</param>
    /// <param name="assembliesToScan">Assemblies to scan for types.</param>
    /// <param name="targetNamespaces">Namespaces within the assemblies to include in the scan.</param>
    /// <param name="defaultLifetime">The default lifetime scope for registered components (can be overridden by marker interfaces like ISingletonDependency).</param>
    /// <param name="registerAsSelf">If true, registers the concrete type itself in addition to the conventional interface.</param>
    /// <returns>The ContainerBuilder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if builder, assembliesToScan, or targetNamespaces are null.</exception>
    /// <exception cref="ArgumentException">Thrown if assembliesToScan or targetNamespaces are empty.</exception>
    public static ContainerBuilder RegisterServicesByConvention(
        this ContainerBuilder builder,
        IEnumerable<Assembly> assembliesToScan,
        IEnumerable<string> targetNamespaces,
        Lifetimes defaultLifetime = Lifetimes.PerLifetimeScope,
        bool registerAsSelf = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(assembliesToScan);
        ArgumentNullException.ThrowIfNull(targetNamespaces);

        Assembly[] assembliesArray = assembliesToScan as Assembly[] ?? assembliesToScan.ToArray();
        if (assembliesArray.Length == 0) throw new ArgumentException("At least one assembly must be provided to scan.", nameof(assembliesToScan));

        string[] namespacesArray = targetNamespaces as string[] ?? targetNamespaces.ToArray();
        if (namespacesArray.Length == 0) throw new ArgumentException("At least one target namespace must be provided.", nameof(targetNamespaces));

        var registrationBuilder = builder.RegisterAssemblyTypes(assembliesArray)
            .Where(type =>
                type.IsPublic &&
                type.IsClass &&
                !type.IsAbstract &&
                !type.IsGenericTypeDefinition &&
                namespacesArray.Any(ns => type.Namespace?.StartsWith(ns, StringComparison.Ordinal) ?? false))
            .As(SelectConventionalInterface);

        if (registerAsSelf)
        {
            registrationBuilder = registrationBuilder.AsSelf();
        }

        // 4. Apply Lifetime (with potential override via marker interfaces)
        ApplyLifetime(registrationBuilder, defaultLifetime);

        // 5. Enable Interface Interception (for attribute-based AOP)
        registrationBuilder.EnableInterfaceInterceptors();

        return builder; // Return original builder for chaining other registrations
    }

    /// <summary>
    /// Helper to select the conventional interface (IClassName) for registration,
    /// excluding common non-service/marker interfaces.
    /// </summary>
    private static IEnumerable<Type> SelectConventionalInterface(Type implementationType)
    {
        Type[] interfacesToExclude = [typeof(IDisposable)];

        string expectedInterfaceName = $"I{implementationType.Name}";
        var interfaces = implementationType.GetInterfaces();

        Type? conventionalInterface = interfaces.FirstOrDefault(i =>
            i.Name == expectedInterfaceName &&
            i.IsPublic &&
            !interfacesToExclude.Contains(i));

        if (conventionalInterface != null)
        {
            return [conventionalInterface];
        }

        return Type.EmptyTypes;
    }

    /// <summary>
    /// Applies lifetime scope based on marker interfaces or the default.
    /// </summary>
    private static void ApplyLifetime<TActivatorData, TRegistrationStyle>(
        IRegistrationBuilder<object, TActivatorData, TRegistrationStyle> registrationBuilder,
        Lifetimes defaultLifetime)
    {

        switch (defaultLifetime)
        {
            case Lifetimes.Singleton:
                registrationBuilder.SingleInstance();
                break;
            case Lifetimes.PerDependency:
                registrationBuilder.InstancePerDependency();
                break;
            case Lifetimes.PerRequest:
                registrationBuilder.InstancePerRequest();
                break;
            case Lifetimes.PerLifetimeScope:
            default:
                registrationBuilder.InstancePerLifetimeScope();
                break;
        }

    }
}
