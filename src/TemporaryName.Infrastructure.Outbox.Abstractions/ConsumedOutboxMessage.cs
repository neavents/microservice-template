namespace TemporaryName.Infrastructure.Outbox.Abstractions;

/// <summary>
/// Represents a message consumed from the outbox source, ready for relaying.
/// </summary>
public class ConsumedOutboxMessage
{
    /// <summary>
    /// Identifier of the original outbox message (e.g., from the outbox database table).
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// Fully qualified name of the event type.
    /// This will be used by the publisher to determine the .NET type for deserialization.
    /// </summary>
    public required string EventTypeName { get; init; }

    /// <summary>
    /// Serialized event data (typically JSON string).
    /// </summary>
    public required string Payload { get; init; }

    /// <summary>
    /// Original headers associated with the message, like CorrelationId.
    /// These should be propagated to the message bus.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    /// <summary>
    /// Delegate to acknowledge successful processing of this message.
    /// For Kafka, this would typically commit the offset.
    /// </summary>
    public required Func<CancellationToken, Task> AcknowledgeAsync { get; init; }

    /// <summary>
    /// Delegate to signal that processing of this message has terminally failed.
    /// For Kafka, this might involve logging the failure and deciding not to commit the offset,
    /// or triggering a move to a Dead Letter Queue (DLQ) if configured.
    /// </summary>
    public required Func<Exception, CancellationToken, Task> FailAsync { get; init; }
}