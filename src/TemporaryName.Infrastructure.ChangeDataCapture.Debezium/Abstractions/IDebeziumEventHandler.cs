using Confluent.Kafka;

namespace TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Abstractions;

public interface IDebeziumEventHandler<TMessageKey, TMessageData>
    // where TMessageData : global::Avro.Specific.ISpecificRecord // If TData is always Avro
{
    /// <summary>
    /// Handles a deserialized Debezium event.
    /// </summary>
    /// <param name="rawKafkaMessage">The original Kafka message, useful for headers or manual offset management.</param>
    /// <param name="typedDebeziumPayload">The Debezium envelope containing the typed TMessageData.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleEventAsync(
        ConsumeResult<TMessageKey, DebeziumPayload<TMessageData>> typedConsumeResult,
        CancellationToken cancellationToken);
}