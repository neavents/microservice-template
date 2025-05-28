using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes; // For AvroDeserializer
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Abstractions;
using TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Models;
using TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Settings;

namespace TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Services;

// TMessageKey: Kafka message key type (e.g., string, or Avro Key type)
// TMessageData: Your business data type within the Debezium payload (e.g., UserAvro, OrderAvro)
// THandler: Your specific implementation of IDebeziumEventHandler
public class GenericDebeziumConsumer<TMessageKey, TMessageData, THandler> : BackgroundService
    where THandler : IDebeziumEventHandler<TMessageKey, TMessageData>
    where TMessageKey : class, Avro.Specific.ISpecificRecord
    where TMessageData : class, Avro.Specific.ISpecificRecord
{
    private readonly string _consumerName; // For logging and potentially named options
    private readonly ILogger<GenericDebeziumConsumer<TMessageKey, TMessageData, THandler>> _logger;
    private readonly KafkaConsumerSettings _settings;
    private readonly THandler _eventHandler;
    private readonly IConsumer<TMessageKey, DebeziumPayload<TMessageData>> _consumer;
    private readonly IKafkaProducer<TMessageKey, string>? _dlqProducer;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly ISchemaRegistryClient _schemaRegistryClient;

    public GenericDebeziumConsumer(
        string consumerName,
        ILogger<GenericDebeziumConsumer<TMessageKey, TMessageData, THandler>> logger,
        IOptionsMonitor<KafkaConsumerSettings> settingsMonitor,
        THandler eventHandler,
        // ISchemaRegistryClient schemaRegistryClient, // Could be injected directly
        IKafkaProducer<TMessageKey, string>? dlqProducer = null)
    {
        _consumerName = consumerName;
        _logger = logger;
        _settings = settingsMonitor.Get(_consumerName);
        _eventHandler = eventHandler;
        _dlqProducer = dlqProducer;

        _logger.LogInformation("[{ConsumerName}] Initializing Debezium Consumer for GroupId: {GroupId}, Topics: {Topics}", _consumerName, _settings.GroupId, string.Join(",", _settings.TopicNames));

        var schemaRegistryConfig = new SchemaRegistryConfig { Url = _settings.SchemaRegistryUrl };
        if (!string.IsNullOrEmpty(_settings.SchemaRegistryBasicAuthUserInfo))
        {
            schemaRegistryConfig.BasicAuthUserInfo = _settings.SchemaRegistryBasicAuthUserInfo;
        }

        _schemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.GroupId,
            AutoOffsetReset = _settings.AutoOffsetResetEnum,
            EnableAutoCommit = _settings.EnableAutoCommit, // false for manual commits
            MaxPollIntervalMs = _settings.MaxPollIntervalMs,
            // StatisticsIntervalMs = 30000, // Example: emit stats every 30s
            // TODO: Map all relevant security settings from _settings to consumerConfig
            // e.g., SecurityProtocol, SaslMechanism, SaslUsername, SaslPassword, SslCaLocation etc.
        };

        // Assuming TMessageKey and DebeziumPayload<TMessageData> are Avro generated types
        // If TMessageKey is primitive (e.g. string, int), use appropriate default deserializer
        var consumerBuilder = new ConsumerBuilder<TMessageKey, DebeziumPayload<TMessageData>>(consumerConfig)
            .SetErrorHandler((_, e) => _logger.LogError(e, "[{ConsumerName}] Kafka Consumer Error: {Reason}. IsFatal: {IsFatal}", _consumerName, e.Reason, e.IsFatal))
            .SetStatisticsHandler((_, json) => _logger.LogDebug("[{ConsumerName}] Kafka Consumer Statistics: {StatsJson}", _consumerName, json))
            .SetLogHandler((_, logMessage) => _logger.Log(logMessage.LevelAsLogLevel(), "[{ConsumerName}] Kafka Client Log: [{Source}] {Message}", _consumerName, logMessage.Name, logMessage.Message));


        _logger.LogInformation("[{ConsumerName}] Setting Avro Key Deserializer for type {KeyType}", _consumerName, typeof(TMessageKey).FullName);
        consumerBuilder.SetKeyDeserializer(new AvroDeserializer<TMessageKey>(_schemaRegistryClient).AsSyncOverAsync());

        // Value Deserializer for DebeziumPayload<TMessageData> (assumed to be an Avro generated type itself)
        // This implies that your Debezium connector is configured with AvroConverter for the *value*
        // and the Avro schema defines the DebeziumPayload structure with an embedded TMessageData schema.
        _logger.LogInformation("[{ConsumerName}] Setting Avro Value Deserializer for type DebeziumPayload<{ValueType}>", _consumerName, typeof(TMessageData).FullName);
        consumerBuilder.SetValueDeserializer(new AvroDeserializer<DebeziumPayload<TMessageData>>(_schemaRegistryClient).AsSyncOverAsync());

        _consumer = consumerBuilder.Build();

        _retryPolicy = Policy
            .Handle<Exception>(ex => !(ex is OperationCanceledException || ex is Avro.AvroException || ex is SchemaRegistryException)) // Don't retry Avro/Schema errors
            .WaitAndRetryAsync(
                _settings.HandlerMaxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(_settings.HandlerRetryBaseDelaySeconds, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    string? msgKey = context.Contains("MessageKey") ? context["MessageKey"]?.ToString() : "N/A";
                    _logger.LogWarning(exception,
                        "[{ConsumerName}] Error in event handler for message Key: {Key}. Retry attempt {RetryCount}/{MaxAttempts}. Waiting {TimeSpan}...",
                        _consumerName, msgKey, retryCount, _settings.HandlerMaxRetryAttempts, timeSpan);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _consumer.Subscribe(_settings.TopicNames);
            _logger.LogInformation("[{ConsumerName}] Consumer started. Subscribed to topics: {Topics}", _consumerName, string.Join(", ", _settings.TopicNames));

            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessNextMessageWithRetriesAndDlq(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[{ConsumerName}] Consumer stopping due to cancellation request.", _consumerName);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[{ConsumerName}] Unhandled exception in ExecuteAsync. Consumer is stopping.", _consumerName);
        }
        finally
        {
            _logger.LogInformation("[{ConsumerName}] Consumer shutting down. Closing Kafka connection.", _consumerName);
            _consumer.Close(); // This also handles leaving the consumer group gracefully
        }
    }

    private async Task ProcessNextMessageWithRetriesAndDlq(CancellationToken stoppingToken)
    {
        ConsumeResult<TMessageKey, DebeziumPayload<TMessageData>>? consumeResult = null;
        try
        {
            consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(_settings.ConsumeTimeoutMs));
            if (consumeResult == null) return; // Timeout
            if (consumeResult.IsPartitionEOF)
            {
                _logger.LogTrace("[{ConsumerName}] Reached end of partition {TopicPartition}", _consumerName, consumeResult.TopicPartition);
                return;
            }

            _logger.LogDebug("[{ConsumerName}] Received message. Key: {Key}, Offset: {Offset}, Value Type: {ValueType}",
                _consumerName, consumeResult.Message.Key, consumeResult.TopicPartitionOffset,
                consumeResult.Message.Value?.GetType().FullName ?? "null");

            if (consumeResult.Message.Value == null && consumeResult.Message.Key != null)
            {
                _logger.LogInformation("[{ConsumerName}] Received tombstone message for Key: {Key} at {TopicPartitionOffset}. Processing as delete.",
                                     _consumerName, consumeResult.Message.Key, consumeResult.TopicPartitionOffset);
                // Construct a minimal DebeziumPayload for tombstones if handler expects it
                // Or, if IDebeziumEventHandler has a specific tombstone method, call that.
                // For now, assume handler checks for null Before/After and Op='d'.
                // The Avro deserializer might return a default-constructed DebeziumPayload if the value was null.
                // If Avro schema doesn't allow null for the whole payload, this path might not be hit
                // and instead a DeserializationException would occur.
            }

            // If consumeResult.Message.Value is null here, it's a tombstone (Debezium often sends key + null value).
            // The AvroDeserializer might handle this by returning a default/null object or throwing.
            // If it returns null, the handler must be prepared.
            // If it throws, the catch block below handles it.

            DebeziumPayload<TMessageData> payload = consumeResult.Message.Value; // Already deserialized by Confluent.Kafka

            // If payload is null after Avro deserialization (for a Kafka null value / tombstone)
            // we might need to create a synthetic one for the handler if it doesn't expect nulls
            if (payload == null && consumeResult.Message.Key != null)
            {
                _logger.LogInformation("[{ConsumerName}] Message value deserialized to null (likely tombstone). Key: {Key}", _consumerName, consumeResult.Message.Key);
                // Create a synthetic payload if your handler needs it, e.g. to identify operation type.
                // This part depends heavily on how your THandler and Avro schemas are designed for tombstones.
                // For now, let's assume the handler can deal with payload being null or having Op='d'.
                // A common pattern is for the DebeziumPayload Avro schema to have Op as a field.
            }


            Context pollyCtx = new Context($"MessageHandler-{_consumerName}-{consumeResult.TopicPartitionOffset}")
            {
                ["MessageKey"] = consumeResult.Message.Key,
                ["Topic"] = consumeResult.Topic,
                ["Partition"] = consumeResult.Partition.Value,
                ["Offset"] = consumeResult.Offset.Value
            };

            await _retryPolicy.ExecuteAsync(async (ctx, ct) =>
            {
                await _eventHandler.HandleEventAsync(consumeResult, ct);
            }, pollyCtx, stoppingToken);

            CommitOffset(consumeResult);
        }
        catch (ConsumeException ex) when (ex.Error.IsFatal) // Fatal Kafka errors
        {
            _logger.LogCritical(ex, "[{ConsumerName}] Fatal Kafka ConsumeException. Error: {Reason}. Consumer stopping.", _consumerName, ex.Error.Reason);
            throw; // Propagate to stop the BackgroundService
        }
        catch (ConsumeException ex) // Non-fatal Kafka errors
        {
            _logger.LogError(ex, "[{ConsumerName}] Non-fatal Kafka ConsumeException: {Reason}. Will continue polling.", _consumerName, ex.Error.Reason);
            // No commit, will re-consume on next poll after broker potentially recovers.
        }
        catch (Avro.AvroException avroEx) // Deserialization errors for Avro
        {
            _logger.LogError(avroEx, "[{ConsumerName}] Avro deserialization error. Key: {Key}. Sending to DLQ.",
                             _consumerName, consumeResult?.Message.Key);
            if (consumeResult != null)
            {
                // Send the original raw message if possible, or a structured error.
                // For now, sending a string representation or just a notification.
                // The DLQ producer is IKafkaProducer<TMessageKey, string>
                // We need to get the raw message bytes if we want to send that.
                // This consumer is IConsumer<TMessageKey, DebeziumPayload<TData>>.
                // This means the raw message isn't easily available here if deserialization failed.
                // A common pattern is to have a prior step/consumer that deals with raw bytes.
                // Or, catch DeserializeException from Confluent.Kafka.
                // The Confluent deserializers might throw DeserializeException wrapping AvroException.
                await SendToDlqAsync(
                    consumeResult.Message.Key, // Key might be null or successfully deserialized
                    $"Avro Deserialization Failed: {avroEx.Message}. Original offset: {consumeResult.TopicPartitionOffset}",
                    consumeResult.Message.Headers, // Pass original headers
                    stoppingToken);
                CommitOffset(consumeResult); // Commit after DLQing
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("[{ConsumerName}] Processing loop cancelled by request.", _consumerName);
            throw; // Propagate to stop
        }
        catch (Exception ex) // Catches exceptions from Polly after retries exhausted or non-retryable exceptions from handler
        {
            _logger.LogError(ex, "[{ConsumerName}] Unrecoverable error after retries or non-retryable error in handler for message Key: {Key}. Sending to DLQ.",
                             _consumerName, consumeResult?.Message.Key);
            if (consumeResult != null)
            {
                await SendToDlqAsync(
                    consumeResult.Message.Key,
                    $"Handler Processing Failed: {ex.Message}. Original offset: {consumeResult.TopicPartitionOffset}",
                    consumeResult.Message.Headers,
                    stoppingToken);
                CommitOffset(consumeResult); // Commit after DLQing
            }
            // If consumeResult is null here, it means an error happened before consuming.
        }
    }

    private void CommitOffset(ConsumeResult<TMessageKey, DebeziumPayload<TMessageData>> consumeResult)
    {
        if (!_settings.EnableAutoCommit)
        {
            try
            {
                _consumer.Commit(consumeResult);
                _logger.LogDebug("[{ConsumerName}] Offset {OffsetValue} committed for {TopicPartition}",
                    _consumerName, consumeResult.Offset, consumeResult.TopicPartition);
            }
            catch (KafkaException ex)
            {
                _logger.LogError(ex, "[{ConsumerName}] Failed to commit offset {OffsetValue} for {TopicPartition}. Risk of reprocessing.",
                    _consumerName, consumeResult.Offset, consumeResult.TopicPartition);
            }
        }
    }

    private async Task SendToDlqAsync(TMessageKey? key, string value, Headers? originalHeaders, CancellationToken cancellationToken)
    {
        if (!_settings.DlqEnabled || _dlqProducer == null)
        {
            _logger.LogWarning("[{ConsumerName}] DLQ not enabled/configured. Cannot send message for Key: {Key}", _consumerName, key);
            return;
        }

        // Determine DLQ topic name - this assumes a single topic in _settings.TopicNames or takes the first.
        // Robust DLQ routing might need more context if consuming multiple topics.
        string originalTopic = _settings.TopicNames.FirstOrDefault() ?? "unknown-original-topic";
        string dlqTopic = originalTopic + _settings.DlqTopicSuffix;

        Headers dlqHeaders = new Headers();
        if (originalHeaders != null)
        {
            foreach (var header in originalHeaders)
            {
                dlqHeaders.Add(header); // Copy original headers
            }
        }
        dlqHeaders.Add("X-DLQ-OriginalTopic", System.Text.Encoding.UTF8.GetBytes(originalTopic));
        dlqHeaders.Add("X-DLQ-ErrorReason", System.Text.Encoding.UTF8.GetBytes(value));
        dlqHeaders.Add("X-DLQ-TimestampUtc", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("o")));
        dlqHeaders.Add("X-DLQ-ConsumerGroup", System.Text.Encoding.UTF8.GetBytes(_settings.GroupId));
        dlqHeaders.Add("X-DLQ-ConsumerName", System.Text.Encoding.UTF8.GetBytes(_consumerName));


        var dlqMessage = new Message<TMessageKey, string>
        {
            Key = key, // Original key
            Value = value, // The error reason or original raw message if available as string
            Headers = dlqHeaders
        };

        try
        {
            _logger.LogInformation("[{ConsumerName}] Sending message with Key: {Key} to DLQ topic: {DlqTopic}. Reason: {Reason}",
                _consumerName, key, dlqTopic, value.Substring(0, Math.Min(value.Length, 100))); // Log snippet of reason
            await _dlqProducer.ProduceAsync(dlqTopic, dlqMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ConsumerName}] CRITICAL: Failed to send message with Key: {Key} to DLQ topic {DlqTopic}",
                _consumerName, key, dlqTopic);
            // This is a severe issue. The message is lost if DLQ fails.
        }
    }


    public override void Dispose()
    {
        _logger.LogInformation("[{ConsumerName}] Disposing consumer.", _consumerName);
        _consumer.Close(); // Important: This will commit offsets if pending and leave the consumer group gracefully.
        _consumer.Dispose();
        _dlqProducer?.Dispose();
        _schemaRegistryClient?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}

// Helper for logging levels from Confluent.Kafka.LogMessage
public static class KafkaLogExtensions
{
    public static LogLevel LevelAsLogLevel(this LogMessage logMessage) => logMessage.Level switch
    {
        SyslogLevel.Emergency => LogLevel.Critical,
        SyslogLevel.Alert => LogLevel.Critical,
        SyslogLevel.Critical => LogLevel.Critical,
        SyslogLevel.Error => LogLevel.Error,
        SyslogLevel.Warning => LogLevel.Warning,
        SyslogLevel.Notice => LogLevel.Information,
        SyslogLevel.Info => LogLevel.Information,
        SyslogLevel.Debug => LogLevel.Debug,
        _ => LogLevel.None
    };
}