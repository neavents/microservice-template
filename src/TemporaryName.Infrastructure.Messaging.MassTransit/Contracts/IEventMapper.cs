using Google.Protobuf;
using TemporaryName.Domain.Primitives.DomainEvent; // Assuming IDomainEvent is here

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Contracts;

/// <summary>
/// Defines a contract for mapping a POCO domain event to a Protobuf integration event.
/// Implementations of this interface will be discovered and used by the MassTransitIntegrationEventPublisher.
/// Implementations should be registered in the DI container.
/// </summary>
/// <typeparam name="TDomainEvent">The type of the POCO domain event.</typeparam>
/// <typeparam name="TIntegrationEvent">The type of the Protobuf integration event, must implement Google.Protobuf.IMessage.</typeparam>
public interface IEventMapper<in TDomainEvent, out TIntegrationEvent>
    where TDomainEvent : IDomainEvent // Your base domain event interface
    where TIntegrationEvent : IMessage // Google.Protobuf.IMessage
{
    /// <summary>
    /// Maps a POCO domain event to its corresponding Protobuf integration event.
    /// </summary>
    /// <param name="domainEvent">The POCO domain event instance.</param>
    /// <returns>The mapped Protobuf integration event.</returns>
    TIntegrationEvent Map(TDomainEvent domainEvent);
}