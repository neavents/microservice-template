using Autofac;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace TemporaryName.Infrastructure.Security.Authorization.Extensions;

public class AuthorizationModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
               .AssignableTo<IAuthorizationHandler>()
               .As<IAuthorizationHandler>()
               .SingleInstance();
    }
}
