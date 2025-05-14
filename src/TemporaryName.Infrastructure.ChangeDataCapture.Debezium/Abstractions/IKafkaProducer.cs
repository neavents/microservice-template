using Confluent.Kafka;

namespace TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Abstractions;

public interface IKafkaProducer<TKey, TValue> : IDisposable
{
    Task ProduceAsync(string topic, Message<TKey, TValue> message, CancellationToken cancellationToken);
}