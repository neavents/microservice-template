namespace TemporaryName.Application.Contracts.Abstractions.Messaging;

/// <summary>
/// Defines the contract for publishing integration events to the message bus.
/// Implementations of this interface are responsible for serializing the event
/// and sending it via the configured message broker (e.g., RabbitMQ, Kafka using MassTransit).
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publishes an integration event.
    /// </summary>
    /// <param name="eventTypeName">The fully qualified name of the event type. Used for deserialization mapping.</param>
    /// <param name="payload">The serialized event data (typically JSON).</param>
    /// <param name="headers">A dictionary of headers to be included with the message on the bus (e.g., CorrelationId, OriginalOutboxId).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous publishing operation.</returns>
    Task PublishAsync(
        string eventTypeName,
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken);
}