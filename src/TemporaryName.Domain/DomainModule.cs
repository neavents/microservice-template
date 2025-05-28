using System;
using System.Reflection;
using Autofac;
using TemporaryName.Common.Autofac;

namespace TemporaryName.Domain;

public class DomainModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        IEnumerable<Assembly> assemblies = [ThisAssembly, typeof().Assembly];
        IEnumerable<string> namespaces = ["TemporaryName.Domain.Services"];

        builder.RegisterServicesByConvention(assemblies, namespaces);
    }
}
