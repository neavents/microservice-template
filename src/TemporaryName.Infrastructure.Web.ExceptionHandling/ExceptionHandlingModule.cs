using System;
using Autofac;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Services;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling;

public class ExceptionHandlingModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register the ProblemDetailsFactory. It depends on IEnumerable<IExceptionProblemDetailsMapper>.
        builder.RegisterType<ProblemDetailsFactory>()
               .AsSelf() // Or as IProblemDetailsFactory if you define an interface
               .InstancePerLifetimeScope(); // Or Singleton if mappers are all stateless

        // Register all types that implement IExceptionProblemDetailsMapper from this assembly.
        // These mappers will be injected into ProblemDetailsFactory.
        // Mappers are typically stateless and can be singletons.
        builder.RegisterAssemblyTypes(ThisAssembly)
               .AssignableTo<IExceptionProblemDetailsMapper>()
               .As<IExceptionProblemDetailsMapper>()
               .SingleInstance(); // Assuming mappers are stateless
    }
}
