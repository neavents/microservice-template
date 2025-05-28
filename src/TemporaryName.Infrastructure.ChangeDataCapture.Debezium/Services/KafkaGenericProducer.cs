using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Abstractions;
using TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Settings;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

namespace TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Services;

public class KafkaGenericProducer<TKey, TValue> : IKafkaProducer<TKey, TValue>
{
    private readonly IProducer<TKey, TValue> _producer;
    private readonly ILogger<KafkaGenericProducer<TKey, TValue>> _logger;
    private readonly KafkaProducerSettings _settings;
    private bool _disposed;

    public KafkaGenericProducer(
        IOptionsMonitor<KafkaProducerSettings> producerSettingsMonitor,
        string producerName,
        ILogger<KafkaGenericProducer<TKey, TValue>> logger,
        ISchemaRegistryClient? schemaRegistryClient = null)
    {
        ArgumentNullException.ThrowIfNull(producerSettingsMonitor);

        _logger = logger;
        _settings = producerSettingsMonitor.Get(producerName);

        ProducerConfig config = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            ClientId = $"{_settings.ClientId}-{Guid.NewGuid().ToString()[..8]}",
            Acks = Enum.TryParse<Acks>(_settings.Acks, true, out var acksEnum) ? acksEnum : Confluent.Kafka.Acks.All,
            SecurityProtocol = _settings.SecurityProtocol,
            SaslMechanism = _settings.SaslMechanism,
            SaslUsername = _settings.SaslUsername,
            SaslPassword = _settings.SaslPassword,
            SslCaLocation = _settings.SslCaLocation,
            SslCertificateLocation = _settings.SslCertificateLocation,
            SslKeyLocation = _settings.SslKeyLocation,
            SslKeyPassword = _settings.SslKeyPassword
        };

        var builder = new ProducerBuilder<TKey, TValue>(config);

        if (typeof(TValue).IsAssignableTo(typeof(Avro.Specific.ISpecificRecord)) && schemaRegistryClient != null)
        {
            _logger.LogInformation("Configuring Avro serializer for producer {ProducerName}, type {ValueType}", producerName, typeof(TValue).Name);
            builder.SetValueSerializer(new AvroSerializer<TValue>(schemaRegistryClient));
        }
        // else if TValue is string, no specific serializer needed by default.
        // else if TValue is byte[], no specific serializer needed.

        _producer = builder.Build();
    }

    public async Task ProduceAsync(string topic, Message<TKey, TValue> message, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            DeliveryResult<TKey, TValue> deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Message delivered to {TopicPartitionOffset} for key {Key}", deliveryResult.TopicPartitionOffset, message.Key);
        }
        catch (ProduceException<TKey, TValue> e)
        {
            _logger.LogError(e, "Delivery failed for message key {Key} to topic {Topic}: {Reason}", message.Key, topic, e.Error.Reason);
            throw;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if(_disposed){
            return;
        }

        if (disposing && _producer is not null)
        {
            try
            {
                _producer.Flush(TimeSpan.FromSeconds(10));
            }
            catch (ObjectDisposedException)
            {
                _logger.LogError("{className} attempted to flush an already disposed object ({disposedClassName})", nameof(KafkaGenericProducer<TKey, TValue>), nameof(IProducer<TKey, TValue>));
            }
            catch (Exception ex)
            {
                _logger.LogError("{className} error during object ({disposedClassName}) flush in Dispose: {errName} - {errMessage}", nameof(KafkaGenericProducer<TKey, TValue>), nameof(IProducer<TKey, TValue>), ex.GetType().FullName, ex.Message);
                _logger.LogTrace("Stack Trace:\n{strace}", ex.StackTrace);
            }
            finally
            {
                _producer.Dispose();
            }
        }
        _disposed = true;
    }
}