// src/TemporaryName.Infrastructure.ChangeDataCapture.Debezium/Outbox/KafkaOutboxMessageProcessor.cs
using Confluent.Kafka;
using Confluent.SchemaRegistry; // For SchemaRegistryConfig
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TemporaryName.Contracts.Proto.Outbox.V1; // Your generated OutboxMessageProto
using TemporaryName.Infrastructure.Outbox.Abstractions;
using TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Kafka; // For KafkaConsumerCore
using TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Settings;
using Confluent.SchemaRegistry.Serdes;
using Confluent.Kafka.SyncOverAsync;

namespace TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Outbox;

/// <summary>
/// Implements IOutboxMessageSource by using KafkaConsumerCore to consume Protobuf-encoded
/// OutboxMessageProto messages from Kafka (published by Debezium).
/// It then transforms these raw messages into the ConsumedOutboxMessage abstraction
/// required by the OutboxEventRelayService.
/// </summary>
public class KafkaOutboxMessageProcessor : IOutboxMessageSource // IDisposable is inherited
{
    private readonly ILogger<KafkaOutboxMessageProcessor> _logger;
    private readonly KafkaOutboxConsumerSettings _settings;
    private readonly KafkaConsumerCore<string, OutboxMessageProto>? _consumerCore; // Nullable if not enabled
    private readonly CancellationTokenSource _internalShutdownCts = new();
    private bool _isDisposed;
    private readonly string _instanceId = Guid.NewGuid().ToString("N").Substring(0, 8);

    // Optional: DLT Producer
    // private readonly IProducer<string, byte[]>? _dltProducer;
    // private readonly string? _dltTopicName;

    public KafkaOutboxMessageProcessor(
        ILogger<KafkaOutboxMessageProcessor> logger,
        IOptions<KafkaOutboxConsumerSettings> settingsOptions
        // Optional: IProducer<string, byte[]> dltProducer // If DLT is handled here
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settingsOptions?.Value ?? throw new ArgumentNullException(nameof(settingsOptions));

        if (!_settings.Enabled)
        {
            _logger.LogInformation("[KOMP-{InstanceId}] KafkaOutboxMessageProcessor is disabled by configuration.", _instanceId);
            return;
        }

        // --- Configure KafkaConsumerCore ---
        var consumerConfig = new ConsumerConfig // Duplicating some config here, could be refactored further
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.GroupId,
            AutoOffsetReset = _settings.AutoOffsetResetEnum,
            EnableAutoCommit = _settings.EnableAutoCommit, // MUST BE FALSE
            SessionTimeoutMs = _settings.SessionTimeoutMs,
            MaxPollIntervalMs = _settings.MaxPollIntervalMs,
            FetchMinBytes = _settings.FetchMinBytes,
            FetchWaitMaxMs = _settings.FetchMaxWaitMs,
            ClientId = string.IsNullOrWhiteSpace(_settings.ClientId) ? $"outbox-proc-{_settings.GroupId}-{_instanceId}" : _settings.ClientId,
            EnablePartitionEof = true,
        };
        // Apply security from _settings to consumerConfig (similar to KafkaConsumerCore's constructor)
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


        SchemaRegistryConfig? schemaRegistryConfig = null;
        if (!string.IsNullOrWhiteSpace(_settings.SchemaRegistryUrl))
        {
            schemaRegistryConfig = new SchemaRegistryConfig { Url = _settings.SchemaRegistryUrl };
            if (_settings.SchemaRegistryMaxCachedSchemas.HasValue) schemaRegistryConfig.MaxCachedSchemas = _settings.SchemaRegistryMaxCachedSchemas;
            if (!string.IsNullOrWhiteSpace(_settings.SchemaRegistryBasicAuthUserInfo))
                schemaRegistryConfig.BasicAuthUserInfo = _settings.SchemaRegistryBasicAuthUserInfo;
        }
        else
        {
            _logger.LogError("[KOMP-{InstanceId}] SchemaRegistryUrl is NOT configured. Protobuf deserialization will fail.", _instanceId);
            throw new InvalidOperationException("SchemaRegistryUrl is required for KafkaOutboxMessageProcessor when using Protobuf.");
        }

        try
        {
            _consumerCore = new KafkaConsumerCore<string, OutboxMessageProto>(
                consumerConfig,
                _settings.OutboxKafkaTopic,
                (srClient) => new ProtobufDeserializer<OutboxMessageProto>(srClient).AsSyncOverAsync(),
                Deserializers.Utf8, // Key deserializer
                _logger, // Pass a compatible ILogger, could be _logger itself or a category-specific one
                _instanceId + "-Core",
                schemaRegistryConfig
            );
            _logger.LogInformation("[KOMP-{InstanceId}] KafkaOutboxMessageProcessor initialized with KafkaConsumerCore.", _instanceId);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[KOMP-{InstanceId}] Failed to initialize KafkaOutboxMessageProcessor due to KafkaConsumerCore failure.", _instanceId);
            // If KafkaConsumerCore throws on init, _consumerCore will be null.
            // The service should not start or ConsumeNextAsync should handle _consumerCore being null.
            _consumerCore = null; // Ensure it's null if init fails
            throw; // Propagate to prevent service startup in bad state
        }

        // Initialize DLT producer if provided/configured
        // if (dltProducer != null && _settings.DltSettings.Enabled) {
        //    _dltProducer = dltProducer;
        //    _dltTopicName = _settings.DltSettings.TopicName;
        // }
    }

    public async Task<ConsumedOutboxMessage?> ConsumeNextAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (_isDisposed || !_settings.Enabled || _consumerCore == null)
        {
            _logger.LogDebug("[KOMP-{InstanceId}] ConsumeNextAsync called but processor is disposed, disabled, or core consumer not initialized.", _instanceId);
            return null;
        }

        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_internalShutdownCts.Token, cancellationToken);
        ConsumeResult<string, OutboxMessageProto>? consumeResult = null;

        try
        {
            // ConsumeRaw might throw if Kafka client has fatal error, or return null for non-fatal / timeout
            consumeResult = _consumerCore.ConsumeRaw(timeout, linkedCts.Token);

            if (linkedCts.Token.IsCancellationRequested)
            {
                _logger.LogInformation("[KOMP-{InstanceId}] Consumption cancelled by request.", _instanceId);
                return null;
            }

            if (consumeResult == null) // Timeout or non-fatal consume error handled by KafkaConsumerCore
            {
                _logger.LogTrace("[KOMP-{InstanceId}] No message received from KafkaConsumerCore (timeout or recoverable error).", _instanceId);
                return null;
            }

            if (consumeResult.IsPartitionEOF)
            {
                _logger.LogTrace("[KOMP-{InstanceId}] Reached end of partition {TopicPartitionOffset}.", _instanceId, consumeResult.TopicPartitionOffset);
                return null;
            }

            if (consumeResult.Message == null || consumeResult.Message.Value == null)
            {
                _logger.LogWarning("[KOMP-{InstanceId}] Consumed tombstone or null message value from {TopicPartitionOffset}, Key: '{Key}'. Acknowledging and skipping.",
                    _instanceId, consumeResult.TopicPartitionOffset, consumeResult.Message?.Key ?? "N/A");
                _consumerCore.Commit(consumeResult); // Acknowledge tombstones directly
                return null;
            }

            OutboxMessageProto outboxProtoEvent = consumeResult.Message.Value;
            _logger.LogDebug("[KOMP-{InstanceId}] Processing OutboxMessageProto: ID='{OutboxId}', EventType='{EventTypeFqn}' from {TopicPartitionOffset}.",
                _instanceId, outboxProtoEvent.Id, outboxProtoEvent.EventTypeFqn, consumeResult.TopicPartitionOffset);

            Dictionary<string, string> combinedHeaders = ExtractHeaders(consumeResult, outboxProtoEvent);

            return new ConsumedOutboxMessage
            {
                MessageId = outboxProtoEvent.Id,
                EventTypeName = outboxProtoEvent.EventTypeFqn,
                Payload = outboxProtoEvent.PayloadJson,
                Headers = combinedHeaders,
                AcknowledgeAsync = (ct) => HandleAcknowledgeAsync(consumeResult, outboxProtoEvent.Id, ct),
                FailAsync = (ex, ct) => HandleFailAsync(consumeResult, outboxProtoEvent.Id, ex, ct)
            };
        }
        catch (OperationCanceledException) when (linkedCts.Token.IsCancellationRequested)
        {
            _logger.LogInformation("[KOMP-{InstanceId}] Consumption operation was cancelled.", _instanceId);
            return null;
        }
        catch (KafkaException ex) when (ex.Error.IsFatal) // Fatal errors from ConsumeRaw re-thrown by KafkaConsumerCore
        {
            _logger.LogCritical(ex, "[KOMP-{InstanceId}] Fatal KafkaException during ConsumeNextAsync. Shutting down processor.", _instanceId);
            _internalShutdownCts.Cancel();
            throw;
        }
        catch (Exception ex) // Catch-all for unexpected issues in this layer
        {
            _logger.LogError(ex, "[KOMP-{InstanceId}] Unexpected error in ConsumeNextAsync. Message at {Offset} (if available) might be skipped or retried depending on failure handling.",
                _instanceId, consumeResult?.TopicPartitionOffset);
            // If consumeResult is available, we could attempt to "Fail" it.
            if (consumeResult != null)
            {
                await HandleFailAsync(consumeResult, consumeResult.Message?.Value?.Id ?? "N/A_ErrorInProcessor", ex, cancellationToken);
            }
            return null;
        }
    }

    private Dictionary<string, string> ExtractHeaders(ConsumeResult<string, OutboxMessageProto> consumeResult, OutboxMessageProto outboxProtoEvent)
    {
        Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (consumeResult.Message.Headers != null)
        {
            foreach (IHeader kafkaHeader in consumeResult.Message.Headers)
            {
                try { headers[kafkaHeader.Key] = Encoding.UTF8.GetString(kafkaHeader.GetValueBytes()); }
                catch (Exception ex) { _logger.LogWarning(ex, "[KOMP-{InstanceId}] Could not decode Kafka header '{HeaderKey}'.", _instanceId, kafkaHeader.Key); }
            }
        }

        // Add well-known fields from OutboxMessageProto to headers
        if (!string.IsNullOrWhiteSpace(outboxProtoEvent.CorrelationId)) headers["CorrelationId"] = outboxProtoEvent.CorrelationId;
        if (!string.IsNullOrWhiteSpace(outboxProtoEvent.CausationId)) headers["CausationId"] = outboxProtoEvent.CausationId;
        if (!string.IsNullOrWhiteSpace(outboxProtoEvent.EventId)) headers["X-Original-Event-Id"] = outboxProtoEvent.EventId; // From proto
        if (!string.IsNullOrWhiteSpace(outboxProtoEvent.TenantId)) headers["X-Tenant-Id"] = outboxProtoEvent.TenantId;
        if (!string.IsNullOrWhiteSpace(outboxProtoEvent.UserId)) headers["X-User-Id"] = outboxProtoEvent.UserId;

        headers["X-Original-Outbox-Id"] = outboxProtoEvent.Id;
        headers["X-Original-Aggregate-Type"] = outboxProtoEvent.AggregateType;
        headers["X-Original-Aggregate-Id"] = outboxProtoEvent.AggregateId;
        if (outboxProtoEvent.AggregateVersion != 0) headers["X-Original-Aggregate-Version"] = outboxProtoEvent.AggregateVersion.ToString();
        if (outboxProtoEvent.OccurredAtUtc != null) headers["X-Event-OccurredAtUtc"] = outboxProtoEvent.OccurredAtUtc.ToDateTimeOffset().ToString("o");


        // Add Kafka context headers
        headers["X-Kafka-Topic"] = consumeResult.Topic;
        headers["X-Kafka-Partition"] = consumeResult.Partition.ToString();
        headers["X-Kafka-Offset"] = consumeResult.Offset.ToString();
        headers["X-Kafka-Timestamp"] = consumeResult.Message.Timestamp.UtcDateTime.ToString("o");
        headers["X-Kafka-Key"] = consumeResult.Message.Key;

        return headers;
    }


    private Task HandleAcknowledgeAsync(ConsumeResult<string, OutboxMessageProto> consumeResult, string outboxId, CancellationToken cancellationToken)
    {
        if (_isDisposed || _consumerCore == null) return Task.CompletedTask;
        try
        {
            _consumerCore.Commit(consumeResult);
            _logger.LogInformation("[KOMP-{InstanceId}] Acknowledged (committed) Outbox ID {OutboxId}, Kafka Offset {Offset}.", _instanceId, outboxId, consumeResult.TopicPartitionOffset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[KOMP-{InstanceId}] Failed to acknowledge (commit) Outbox ID {OutboxId}, Kafka Offset {Offset}. This may lead to reprocessing.", _instanceId, outboxId, consumeResult.TopicPartitionOffset);
            // This is critical. If commit fails, the message will be reprocessed.
            // The OutboxEventRelayService's Polly policy might retry the whole operation, including this commit.
            // If this exception is caught by Polly, it might be okay. If not, it's data duplication risk.
            throw; // Re-throw to allow higher-level retry mechanisms (like Polly in relay service) to act.
        }
        return Task.CompletedTask;
    }

    private async Task HandleFailAsync(ConsumeResult<string, OutboxMessageProto> consumeResult, string outboxId, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "[KOMP-{InstanceId}] Processing failed for Outbox ID {OutboxId}, Kafka Offset {Offset}. Offset will NOT be committed, leading to reprocessing.",
            _instanceId, outboxId, consumeResult.TopicPartitionOffset);

        // --- DLT Logic (Conceptual - requires DLT producer injection and configuration) ---
        // if (_dltProducer != null && _dltTopicName != null)
        // {
        //     _logger.LogWarning("[KOMP-{InstanceId}] Attempting to send failed message (Outbox ID {OutboxId}) to DLT: {DltTopicName}", _instanceId, outboxId, _dltTopicName);
        //     // Construct DLT message with error details and original payload
        //     // var dltMessage = CreateDltMessage(consumeResult, exception);
        //     // try
        //     // {
        //     //    await _dltProducer.ProduceAsync(_dltTopicName, dltMessage, cancellationToken);
        //     //    _logger.LogInformation("[KOMP-{InstanceId}] Successfully sent message to DLT. Now committing original offset {Offset} to avoid reprocessing.", _instanceId, consumeResult.TopicPartitionOffset);
        //     //    _consumerCore?.Commit(consumeResult); // Commit original if DLT publish succeeds
        //     //    return; // Successfully handled by DLT
        //     // }
        //     // catch (Exception dltEx)
        //     // {
        //     //    _logger.LogCritical(dltEx, "[KOMP-{InstanceId}] CRITICAL: Failed to send message (Outbox ID {OutboxId}) to DLT {DltTopicName}. Original offset {Offset} NOT committed.", _instanceId, outboxId, _dltTopicName, consumeResult.TopicPartitionOffset);
        //     // }
        // }
        // If no DLT or DLT send fails, the offset is not committed. Message will be reprocessed.
        // The OutboxEventRelayService's Polly policy will retry processing. If all retries fail,
        // this FailAsync will be called again. Without DLT, "poison messages" can block the partition.
        await Task.CompletedTask;
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
            _logger.LogInformation("[KOMP-{InstanceId}] Disposing KafkaOutboxMessageProcessor.", _instanceId);
            _internalShutdownCts.Cancel(); // Signal any ongoing operations
            _consumerCore?.Dispose();
            _internalShutdownCts.Dispose();
        }
        _isDisposed = true;
    }
}
