namespace TemporaryName.Infrastructure.Outbox.Abstractions;

/// <summary>
/// Defines the contract for a source that provides messages from the outbox.
/// This is typically implemented by a Kafka consumer listening to a Debezium topic.
/// </summary>
public interface IOutboxMessageSource : IDisposable
{
    /// <summary>
    /// Asynchronously consumes the next message from the outbox source.
    /// </summary>
    /// <param name="timeout">The maximum time to wait for a message.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the
    /// <see cref="ConsumedOutboxMessage"/> if a message is available within the timeout,
    /// otherwise null.
    /// </returns>
    Task<ConsumedOutboxMessage?> ConsumeNextAsync(TimeSpan timeout, CancellationToken cancellationToken);
}