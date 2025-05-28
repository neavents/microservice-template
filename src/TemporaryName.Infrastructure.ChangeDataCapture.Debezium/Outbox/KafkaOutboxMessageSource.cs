// src/TemporaryName.Infrastructure.ChangeDataCapture.Debezium/Outbox/KafkaOutboxMessageSource.cs
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes; // For ProtobufDeserializer
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text; // For header decoding
using System.Threading;
using System.Threading.Tasks;
using TemporaryName.Contracts.Proto.Outbox.V1; // Your generated OutboxMessageProto
using TemporaryName.Infrastructure.Outbox.Abstractions;
using TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Settings;

namespace TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Outbox;

/// <summary>
/// Implements IOutboxMessageSource by consuming Protobuf-encoded messages from a Kafka topic
/// that is populated by Debezium capturing changes from an outbox table.
/// This class is responsible for Kafka consumer setup, message deserialization,
/// and providing mechanisms for message acknowledgment or failure notification.
/// </summary>
public class KafkaOutboxMessageSource : IOutboxMessageSource // IDisposable is inherited
{
    private readonly ILogger<KafkaOutboxMessageSource> _logger;
    private readonly KafkaOutboxConsumerSettings _settings;
    private IConsumer<string, OutboxMessageProto>? _kafkaConsumer; // Nullable until initialized
    private ISchemaRegistryClient? _schemaRegistryClient; // Nullable, initialized if URL is provided
    private readonly CancellationTokenSource _internalCts = new(); // For coordinating shutdown
    private bool _isDisposed = false;
    private readonly string _instanceId = Guid.NewGuid().ToString("N").Substring(0, 8); // For disambiguating logs

    // Optional: Inject a dedicated producer for sending messages to a Dead Letter Topic (DLT)
    // private readonly IProducer<string, byte[]>? _dltProducer;

    public KafkaOutboxMessageSource(
        ILogger<KafkaOutboxMessageSource> logger,
        IOptions<KafkaOutboxConsumerSettings> settingsOptions
        // Optional: IProducer<string, byte[]> dltProducer // If you want to inject a pre-configured DLT producer
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settingsOptions?.Value ?? throw new ArgumentNullException(nameof(settingsOptions), "KafkaOutboxConsumerSettings cannot be null.");

        if (!_settings.Enabled)
        {
            _logger.LogInformation("[KOMS-{InstanceId}] KafkaOutboxMessageSource is disabled by configuration. No consumer will be initialized.", _instanceId);
            return; // Consumer remains null
        }

        InitializeConsumer();
    }

    private void InitializeConsumer()
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.GroupId,
            AutoOffsetReset = _settings.AutoOffsetResetEnum,
            EnableAutoCommit = _settings.EnableAutoCommit, // MUST BE FALSE for manual commits
            SessionTimeoutMs = _settings.SessionTimeoutMs,
            MaxPollIntervalMs = _settings.MaxPollIntervalMs,
            FetchMinBytes = _settings.FetchMinBytes,
            FetchMaxWaitMs = _settings.FetchMaxWaitMs,
            ClientId = string.IsNullOrWhiteSpace(_settings.ClientId) ? $"outbox-consumer-{_settings.GroupId}-{_instanceId}" : _settings.ClientId,
            EnablePartitionEof = true, // To get notified when end of partition is reached
            // StatisticsIntervalMs = 30000, // Example: emit stats every 30s
            // LogConnectionClose = _settings.LogConnectionClose,
            // LogThreadExit = _settings.LogThreadExit,
            // Debug = _settings.Debug,
        };

        // Apply security settings
        if (_settings.SecurityProtocol.HasValue) consumerConfig.SecurityProtocol = _settings.SecurityProtocol;
        if (_settings.SaslMechanism.HasValue)
        {
            consumerConfig.SaslMechanism = _settings.SaslMechanism;
            consumerConfig.SaslUsername = _settings.SaslUsername;
            consumerConfig.SaslPassword = _settings.SaslPassword;
            if (!string.IsNullOrWhiteSpace(_settings.SaslOauthbearerConfig))
                consumerConfig.SaslOauthbearerConfig = _settings.SaslOauthbearerConfig;
        }
        consumerConfig.SslCaLocation = _settings.SslCaLocation;
        consumerConfig.SslCertificateLocation = _settings.SslCertificateLocation;
        consumerConfig.SslKeyLocation = _settings.SslKeyLocation;
        consumerConfig.SslKeyPassword = _settings.SslKeyPassword;
        if (!string.IsNullOrWhiteSpace(_settings.SslCipherSuites)) consumerConfig.SslCipherSuites = _settings.SslCipherSuites;
        if (!string.IsNullOrWhiteSpace(_settings.SslCurvesList)) consumerConfig.SslCurvesList = _settings.SslCurvesList;
        if (!string.IsNullOrWhiteSpace(_settings.SslSigalgsList)) consumerConfig.SslSigalgsList = _settings.SslSigalgsList;
        if (_settings.EnableSslCertificateVerification.HasValue) consumerConfig.EnableSslCertificateVerification = _settings.EnableSslCertificateVerification;
        if (_settings.SslEndpointIdentificationAlgorithm.HasValue) consumerConfig.SslEndpointIdentificationAlgorithm = _settings.SslEndpointIdentificationAlgorithm;

        if (_settings.CustomConfiguration != null)
        {
            foreach (KeyValuePair<string, string> entry in _settings.CustomConfiguration)
            {
                consumerConfig.Set(entry.Key, entry.Value);
            }
        }

        var consumerBuilder = new ConsumerBuilder<string, OutboxMessageProto>(consumerConfig)
            .SetKeyDeserializer(Deserializers.Utf8); // Assuming Debezium sends string keys (e.g., outbox message ID)

        // Configure Protobuf deserializer with Schema Registry
        if (!string.IsNullOrWhiteSpace(_settings.SchemaRegistryUrl))
        {
            var schemaRegistryConfig = new SchemaRegistryConfig { Url = _settings.SchemaRegistryUrl };
            if (_settings.SchemaRegistryMaxCachedSchemas.HasValue) schemaRegistryConfig.MaxCachedSchemas = _settings.SchemaRegistryMaxCachedSchemas;
            if (!string.IsNullOrWhiteSpace(_settings.SchemaRegistryBasicAuthUserInfo))
            {
                schemaRegistryConfig.BasicAuthUserInfo = _settings.SchemaRegistryBasicAuthUserInfo;
            }
            _schemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);
            consumerBuilder.SetValueDeserializer(new ProtobufDeserializer<OutboxMessageProto>(_schemaRegistryClient).AsSyncOverAsync());
             _logger.LogInformation("[KOMS-{InstanceId}] Configured Protobuf deserializer with Schema Registry at {SchemaRegistryUrl}.", _instanceId, _settings.SchemaRegistryUrl);
        }
        else
        {
            // This is a critical misconfiguration if Protobuf is expected.
            _logger.LogError("[KOMS-{InstanceId}] SchemaRegistryUrl is NOT configured. Protobuf deserialization will likely fail if messages are schema-encoded. Debezium connector for outbox table MUST be configured to produce Protobuf with schema registry for this setup.", _instanceId);
            // Consider throwing an exception here to prevent startup with invalid config,
            // or make SchemaRegistryUrl a 'required' property in settings.
            throw new InvalidOperationException("SchemaRegistryUrl is required for Protobuf deserialization of OutboxMessageProto but was not provided.");
        }

        consumerBuilder
            .SetErrorHandler((consumer, error) => HandleKafkaError(consumer, error, "Consumer"))
            .SetLogHandler((consumer, logMessage) => HandleKafkaLog(consumer, logMessage, "Consumer"))
            .SetStatisticsHandler((consumer, jsonStats) => HandleKafkaStats(consumer, jsonStats, "Consumer"))
            .SetPartitionsAssignedHandler((c, partitions) =>
            {
                _logger.LogInformation("[KOMS-{InstanceId}] Consumer assigned partitions: [{PartitionsInfo}]. Current assignments: [{CurrentAssignments}]",
                    _instanceId,
                    string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]@{p.Offset}")),
                    string.Join(", ", c.Assignment.Select(p => $"{p.Topic}[{p.Partition}]")));
                // If specific offsets are needed on assignment, they can be sought here.
                // However, AutoOffsetResetEnum and stored group offsets usually handle this.
            })
            .SetPartitionsRevokedHandler((c, partitions) =>
            {
                _logger.LogWarning("[KOMS-{InstanceId}] Consumer revoking partitions: [{PartitionsInfo}]. Current assignments before revoke: [{CurrentAssignments}]",
                    _instanceId,
                    string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]@{p.Offset}")),
                    string.Join(", ", c.Assignment.Select(p => $"{p.Topic}[{p.Partition}]")));
                // Opportunity to commit any pending offsets if absolutely necessary before rebalance,
                // but with manual commits, this should ideally be handled per message.
            })
            .SetOffsetsCommittedHandler((c, committedOffsets) => {
                if (committedOffsets.Error is not null && committedOffsets.Error.Code != ErrorCode.NoError) {
                     _logger.LogError("[KOMS-{InstanceId}] Failed to commit offsets: [{ErrorReason}]. Offsets: [{Offsets}]", _instanceId, committedOffsets.Error.Reason, string.Join(", ", committedOffsets.Offsets.Select(o => $"{o.TopicPartition}@{o.Offset}")));
                } else {
                     _logger.LogDebug("[KOMS-{InstanceId}] Successfully committed offsets: [{Offsets}]", _instanceId, string.Join(", ", committedOffsets.Offsets.Select(o => $"{o.TopicPartition}@{o.Offset}")));
                }
            });

        _kafkaConsumer = consumerBuilder.Build();
        _kafkaConsumer.Subscribe(_settings.OutboxKafkaTopic);

        _logger.LogInformation("[KOMS-{InstanceId}] KafkaOutboxMessageSource initialized. Subscribed to topic '{TopicName}' with GroupId '{GroupId}'. BootstrapServers: {BootstrapServers}",
            _instanceId, _settings.OutboxKafkaTopic, _settings.GroupId, _settings.BootstrapServers);
    }


    public async Task<ConsumedOutboxMessage?> ConsumeNextAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (_isDisposed || !_settings.Enabled || _kafkaConsumer == null)
        {
            _logger.LogDebug("[KOMS-{InstanceId}] ConsumeNextAsync called but source is disposed, disabled, or consumer not initialized.", _instanceId);
            return null;
        }

        // Link the external cancellationToken with the internal one for coordinated shutdown.
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_internalCts.Token, cancellationToken);

        ConsumeResult<string, OutboxMessageProto>? consumeResult = null;
        try
        {
            // The Consume call is blocking. It will wait for `timeout` or until a message arrives or an error occurs.
            consumeResult = _kafkaConsumer.Consume(timeout); // No need for Task.Run if this is called by a dedicated BackgroundService thread.

            if (linkedCts.Token.IsCancellationRequested)
            {
                _logger.LogInformation("[KOMS-{InstanceId}] Consumption cancelled by request during or after Consume() call.", _instanceId);
                return null;
            }

            if (consumeResult == null) // Timeout
            {
                _logger.LogTrace("[KOMS-{InstanceId}] Consume timed out after {Timeout}ms. No message received.", _instanceId, timeout.TotalMilliseconds);
                return null;
            }

            if (consumeResult.IsPartitionEOF)
            {
                _logger.LogTrace("[KOMS-{InstanceId}] Reached end of partition {TopicPartitionOffset}. No new messages currently.", _instanceId, consumeResult.TopicPartitionOffset);
                return null;
            }

            // Tombstone message (null value) indicates deletion of the outbox record by Debezium.
            // These should generally be acknowledged and skipped unless specific handling is needed.
            if (consumeResult.Message == null || consumeResult.Message.Value == null)
            {
                _logger.LogWarning("[KOMS-{InstanceId}] Consumed a null message (tombstone for key '{Key}' or unexpected null) from {TopicPartitionOffset}. This will be acknowledged and skipped.",
                    _instanceId, consumeResult.Message?.Key ?? "N/A", consumeResult.TopicPartitionOffset);
                AcknowledgeTombstone(consumeResult);
                return null;
            }

            OutboxMessageProto outboxProtoEvent = consumeResult.Message.Value;
            _logger.LogDebug("[KOMS-{InstanceId}] Received OutboxMessageProto: ID='{OutboxId}', EventType='{EventType}' from {TopicPartitionOffset}. Key: '{KafkaKey}'.",
                _instanceId, outboxProtoEvent.Id, outboxProtoEvent.EventTypeFqn, consumeResult.TopicPartitionOffset, consumeResult.Message.Key);

            // Header extraction from Kafka message (Debezium might add some)
            // And from the OutboxMessageProto itself
            Dictionary<string, string> combinedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (consumeResult.Message.Headers != null)
            {
                foreach (IHeader kafkaHeader in consumeResult.Message.Headers)
                {
                    try
                    {
                        combinedHeaders[kafkaHeader.Key] = Encoding.UTF8.GetString(kafkaHeader.GetValueBytes());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[KOMS-{InstanceId}] Could not decode Kafka header '{HeaderKey}' for Outbox ID {OutboxId}.", _instanceId, kafkaHeader.Key, outboxProtoEvent.Id);
                    }
                }
            }

            // Add key fields from OutboxMessageProto to headers for the relay service
            if (!string.IsNullOrWhiteSpace(outboxProtoEvent.CorrelationId)) combinedHeaders["CorrelationId"] = outboxProtoEvent.CorrelationId;
            if (!string.IsNullOrWhiteSpace(outboxProtoEvent.CausationId)) combinedHeaders["CausationId"] = outboxProtoEvent.CausationId;
            if (!string.IsNullOrWhiteSpace(outboxProtoEvent.EventId)) combinedHeaders["X-Original-Event-Id"] = outboxProtoEvent.EventId;
            if (!string.IsNullOrWhiteSpace(outboxProtoEvent.TenantId)) combinedHeaders["X-Tenant-Id"] = outboxProtoEvent.TenantId;

            combinedHeaders["X-Original-Outbox-Id"] = outboxProtoEvent.Id;
            combinedHeaders["X-Original-Aggregate-Type"] = outboxProtoEvent.AggregateType;
            combinedHeaders["X-Original-Aggregate-Id"] = outboxProtoEvent.AggregateId;
            combinedHeaders["X-Kafka-Topic"] = consumeResult.Topic;
            combinedHeaders["X-Kafka-Partition"] = consumeResult.Partition.ToString();
            combinedHeaders["X-Kafka-Offset"] = consumeResult.Offset.ToString();


            return new ConsumedOutboxMessage
            {
                MessageId = outboxProtoEvent.Id, // ID from the outbox_messages table
                EventTypeName = outboxProtoEvent.EventTypeFqn,
                Payload = outboxProtoEvent.PayloadJson, // JSON payload of the actual domain event
                Headers = combinedHeaders,
                AcknowledgeAsync = (ct) => HandleAcknowledgeAsync(consumeResult, outboxProtoEvent.Id, ct),
                FailAsync = (ex, ct) => HandleFailAsync(consumeResult, outboxProtoEvent.Id, ex, ct)
            };
        }
        catch (ConsumeException ex) // Errors during the .Consume() call itself
        {
            _logger.LogError(ex, "[KOMS-{InstanceId}] Kafka ConsumeException while consuming from '{TopicName}': {Reason}. Error Code: {ErrorCode}. IsFatal: {IsFatal}. Will attempt to recover unless fatal.",
                _instanceId, _settings.OutboxKafkaTopic, ex.Error.Reason, ex.Error.Code, ex.Error.IsFatal);
            if (ex.Error.IsFatal)
            {
                _logger.LogCritical("[KOMS-{InstanceId}] Fatal Kafka ConsumeException. The consumer may need to be restarted or configuration checked. Triggering internal shutdown.", _instanceId);
                _internalCts.Cancel(); // Signal internal shutdown
                throw; // Re-throw to stop the background service or alert higher levels
            }
            return null; // Non-fatal, allow retry on the next call to ConsumeNextAsync
        }
        catch (OperationCanceledException) when (linkedCts.Token.IsCancellationRequested)
        {
            _logger.LogInformation("[KOMS-{InstanceId}] Kafka consumption in OutboxMessageSource was cancelled by request.", _instanceId);
            return null;
        }
        // Catch deserialization errors specifically if they are not caught by ConsumeException
        catch (InvalidProtoException protoEx) // From Confluent.SchemaRegistry.Serdes.Protobuf
        {
             _logger.LogError(protoEx, "[KOMS-{InstanceId}] Protobuf Deserialization Error for message at {TopicPartitionOffset}. Key: '{Key}'. This message might be malformed or schema incompatible. It will be marked as failed.",
                _instanceId, consumeResult?.TopicPartitionOffset, consumeResult?.Message?.Key);
            if (consumeResult != null) await HandleFailAsync(consumeResult, consumeResult.Message?.Value?.Id ?? "N/A_ProtoError", protoEx, cancellationToken);
            return null;
        }
        catch (Exception ex) // Catch-all for other unexpected errors during consumption/mapping
        {
            _logger.LogError(ex, "[KOMS-{InstanceId}] Unexpected error in KafkaOutboxMessageSource. Key: {Key}, Offset: {Offset}. Message will be marked as failed.",
                _instanceId, consumeResult?.Message?.Key, consumeResult?.TopicPartitionOffset);
            if (consumeResult != null) await HandleFailAsync(consumeResult, consumeResult.Message?.Value?.Id ?? "N/A_GenericError", ex, cancellationToken);
            return null;
        }
    }

    private void AcknowledgeTombstone(ConsumeResult<string, OutboxMessageProto> consumeResult)
    {
        if (_isDisposed || _kafkaConsumer == null || _settings.EnableAutoCommit) return;
        try
        {
            _kafkaConsumer.Commit(consumeResult);
            _logger.LogInformation("[KOMS-{InstanceId}] Successfully committed offset for tombstone message. Key: '{Key}', TopicPartitionOffset: {TopicPartitionOffset}",
                _instanceId, consumeResult.Message?.Key ?? "N/A", consumeResult.TopicPartitionOffset);
        }
        catch (KafkaException e)
        {
            _logger.LogError(e, "[KOMS-{InstanceId}] Failed to commit offset for tombstone message. Key: '{Key}', TopicPartitionOffset: {TopicPartitionOffset}. Error: {ErrorReason}",
                _instanceId, consumeResult.Message?.Key ?? "N/A", consumeResult.TopicPartitionOffset, e.Error.Reason);
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "[KOMS-{InstanceId}] Unexpected error committing offset for tombstone message. Key: '{Key}', TopicPartitionOffset: {TopicPartitionOffset}.",
                _instanceId, consumeResult.Message?.Key ?? "N/A", consumeResult.TopicPartitionOffset);
        }
    }

    private Task HandleAcknowledgeAsync(ConsumeResult<string, OutboxMessageProto> consumeResult, string outboxId, CancellationToken cancellationToken)
    {
        if (_isDisposed || _kafkaConsumer == null || _settings.EnableAutoCommit) return Task.CompletedTask;

        try
        {
            _kafkaConsumer.Commit(consumeResult); // Commits the specific offset of this message
            _logger.LogInformation("[KOMS-{InstanceId}] Successfully committed Kafka offset {Offset} for Outbox ID {OutboxId} on {TopicPartition}.",
                _instanceId, consumeResult.Offset, outboxId, consumeResult.TopicPartition);
        }
        catch (KafkaException e)
        {
            _logger.LogError(e, "[KOMS-{InstanceId}] Failed to commit Kafka offset {Offset} for Outbox ID {OutboxId}. Error: {ErrorReason}. This may lead to reprocessing.",
                _instanceId, consumeResult.Offset, outboxId, e.Error.Reason);
            // Depending on the error, might re-throw or trigger a more severe alert.
            // If ErrorCode is Local_Application or similar, it might be retryable by the caller.
            // If it's a fatal broker error, the consumer might already be in a bad state.
            if (e.Error.IsFatal) _internalCts.Cancel(); // Trigger shutdown for fatal commit errors
            // Consider throwing to allow Polly in OutboxEventRelayService to retry the whole operation including commit
            // throw; // This would make the relay service retry the event processing + commit.
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "[KOMS-{InstanceId}] Unexpected error committing Kafka offset {Offset} for Outbox ID {OutboxId}.",
                _instanceId, consumeResult.Offset, outboxId);
            // throw; // Potentially re-throw
        }
        return Task.CompletedTask;
    }

    private async Task HandleFailAsync(ConsumeResult<string, OutboxMessageProto> consumeResult, string outboxId, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "[KOMS-{InstanceId}] Processing failed for Outbox ID {OutboxId}, EventType: '{EventType}', Kafka Offset {Offset}. Kafka offset will NOT be committed by default, leading to reprocessing unless a DLT is used.",
            _instanceId, outboxId, consumeResult.Message?.Value?.EventTypeFqn ?? "N/A", consumeResult.Offset);

        // --- Enterprise DLQ Strategy ---
        // If a DLT (Dead Letter Topic) producer is configured, send the raw problematic message there.
        // if (_dltProducer != null && _settings.DlqSettings.Enabled)
        // {
        //     try
        //     {
        //         var rawMessageBytes = consumeResult.Message.Value == null ? null : System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(consumeResult.Message.Value); // Or original bytes if available
        //         Message<string, byte[]> dlqMessage = new Message<string, byte[]>
        //         {
        //             Key = consumeResult.Message.Key,
        //             Value = rawMessageBytes, // Send raw bytes or a specific DLT message format
        //             Headers = new Headers
        //             {
        //                 { "X-Dlt-Reason", Encoding.UTF8.GetBytes(exception.Message) },
        //                 { "X-Dlt-Exception-Type", Encoding.UTF8.GetBytes(exception.GetType().FullName!) },
        //                 { "X-Dlt-Original-Topic", Encoding.UTF8.GetBytes(consumeResult.Topic) },
        //                 { "X-Dlt-Original-Partition", Encoding.UTF8.GetBytes(consumeResult.Partition.Value.ToString()) },
        //                 { "X-Dlt-Original-Offset", Encoding.UTF8.GetBytes(consumeResult.Offset.Value.ToString()) },
        //                 { "X-Dlt-Timestamp", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("o")) }
        //             }
        //         };
        //         DeliveryResult<string, byte[]> deliveryResult = await _dltProducer.ProduceAsync(_settings.DlqSettings.TopicName, dlqMessage, cancellationToken);
        //         _logger.LogWarning("[KOMS-{InstanceId}] Sent message (Outbox ID {OutboxId}) to DLT '{DltTopic}' at offset {DltOffset}. Original offset {OriginalOffset} will now be committed.",
        //            _instanceId, outboxId, _settings.DlqSettings.TopicName, deliveryResult.TopicPartitionOffset, consumeResult.Offset);
        //
        //         // IMPORTANT: If successfully sent to DLT, COMMIT the original message's offset to prevent reprocessing from main topic.
        //         await HandleAcknowledgeAsync(consumeResult, outboxId, cancellationToken);
        //         return; // Exit after DLT processing + commit
        //     }
        //     catch (Exception dltEx)
        //     {
        //         _logger.LogCritical(dltEx, "[KOMS-{InstanceId}] FATAL: Failed to send message (Outbox ID {OutboxId}) to DLT '{DltTopic}'. Original offset {OriginalOffset} NOT committed. Message may get stuck or reprocessed.",
        //            _instanceId, outboxId, _settings.DlqSettings.TopicName, consumeResult.Offset);
        //         // Do NOT commit original offset if DLT publish fails. Let it be reprocessed.
        //         // This scenario needs careful monitoring and manual intervention strategy.
        //     }
        // }

        // If no DLT or DLT failed, the offset is not committed.
        // The message will be re-consumed after a rebalance or consumer restart.
        // This could lead to a "poison message" scenario if it always fails.
        // Consider implementing a circuit breaker or max reprocessing count at the relay service level
        // or a more sophisticated local "skip" counter with logging if DLT is not an option.
        await Task.CompletedTask;
    }


    private void HandleKafkaError(IConsumer<string, OutboxMessageProto> consumer, Error error, string consumerName)
    {
        // Log all errors, but take specific action for fatal ones.
        LogLevel level = error.IsFatal ? LogLevel.Critical :
                         error.IsBrokerError || error.IsLocalError ? LogLevel.Error : LogLevel.Warning;

        _logger.Log(level, "[KOMS-{InstanceId}] Kafka {ConsumerName} Error: Code={ErrorCode}, Reason='{Reason}', IsBroker={IsBroker}, IsLocal={IsLocal}, IsFatal={IsFatal}",
            _instanceId, consumerName, error.Code, error.Reason, error.IsBrokerError, error.IsLocalError, error.IsFatal);

        if (error.IsFatal)
        {
            // For fatal errors, the consumer is likely no longer usable.
            // Trigger a shutdown of this source to allow the application to handle it (e.g., restart the service).
            _logger.LogCritical("[KOMS-{InstanceId}] Fatal Kafka error encountered. Triggering internal shutdown of KafkaOutboxMessageSource.", _instanceId);
            _internalCts.Cancel();
        }
    }

    private void HandleKafkaLog(IConsumer<string, OutboxMessageProto> consumer, LogMessage logMessage, string consumerName)
    {
        LogLevel logLevel = logMessage.LevelAsLogLevel();
        _logger.Log(logLevel, "[KOMS-{InstanceId}] Kafka {ConsumerName} Client Log [{Source} Lvl {LevelNum} Fac {Facility}]: {Message}",
            _instanceId, consumerName, logMessage.Name, (int)logMessage.Level, logMessage.Facility, logMessage.Message);
    }

    private void HandleKafkaStats(IConsumer<string, OutboxMessageProto> consumer, string jsonStats, string consumerName)
    {
        _logger.LogInformation("[KOMS-{InstanceId}] Kafka {ConsumerName} Statistics: {StatsJson}", _instanceId, consumerName, jsonStats);
        // Here you could parse jsonStats and publish specific metrics to Prometheus/OpenTelemetry, etc.
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            _logger.LogInformation("[KOMS-{InstanceId}] Disposing KafkaOutboxMessageSource.", _instanceId);
            _internalCts.Cancel(); // Signal any ongoing Consume operations to stop

            if (_kafkaConsumer != null)
            {
                try
                {
                    // Close() ensures the consumer leaves the group gracefully and commits final offsets if needed (though we do manual).
                    // It can block for up to session.timeout.ms.
                    _logger.LogInformation("[KOMS-{InstanceId}] Closing Kafka consumer for GroupId '{GroupId}'. This may take a moment...", _instanceId, _settings.GroupId);
                    _kafkaConsumer.Close(); // Blocks until consumer has left group.
                    _logger.LogInformation("[KOMS-{InstanceId}] Kafka consumer closed.", _instanceId);
                }
                catch (KafkaException ex)
                {
                    _logger.LogWarning(ex, "[KOMS-{InstanceId}] KafkaException during consumer Close(): {Reason}", _instanceId, ex.Error.Reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[KOMS-{InstanceId}] Unexpected exception during Kafka consumer Close().", _instanceId);
                }
                finally
                {
                    _kafkaConsumer.Dispose();
                    _kafkaConsumer = null;
                     _logger.LogInformation("[KOMS-{InstanceId}] Kafka consumer disposed.", _instanceId);
                }
            }

            _schemaRegistryClient?.Dispose();
            _schemaRegistryClient = null;

            // _dltProducer?.Dispose(); // If using DLT producer

            _internalCts.Dispose();
        }
        _isDisposed = true;
    }
}

// Helper extension for logging Kafka LogMessage
internal static class KafkaLogMessageExtensions
{
    internal static LogLevel LevelAsLogLevel(this LogMessage logMessage) => logMessage.Level switch
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
