using System;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using TemporaryName.Common.Autofac;

namespace TemporaryName.Application;

public class ApplicationServicesModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        IEnumerable<Assembly> assemblies = [
            ThisAssembly,
            typeof().Assembly,
            ];

        IEnumerable<string> namespaces = [
            "TemporaryName.Application.Services",
            "TemporaryName.Application.Usecases"
        ];
        builder.RegisterServicesByConvention(assemblies, namespaces);
    }
}
