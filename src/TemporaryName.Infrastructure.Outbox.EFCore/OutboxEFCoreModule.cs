using Autofac;
using TemporaryName.Infrastructure.Outbox.EFCore.Interceptors;
using Microsoft.Extensions.Logging; 

namespace TemporaryName.Infrastructure.Outbox.EFCore;

public class OutboxEFCoreModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(c =>
            new ConvertDomainEventsToOutboxMessagesInterceptor(
                c.Resolve<ILogger<ConvertDomainEventsToOutboxMessagesInterceptor>>()
            ))
            .AsSelf()
            .SingleInstance();
    }
}