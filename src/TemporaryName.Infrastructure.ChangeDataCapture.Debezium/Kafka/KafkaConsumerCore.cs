// src/TemporaryName.Infrastructure.ChangeDataCapture.Debezium/Kafka/KafkaConsumerCore.cs
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Kafka;

/// <summary>
/// A core wrapper around the Confluent Kafka IConsumer.
/// Handles consumer configuration, lifecycle, subscription, and raw message consumption.
/// It is generic for key and value types and uses a provided deserializer for the value.
/// </summary>
/// <typeparam name="TKey">The Kafka message key type.</typeparam>
/// <typeparam name="TValue">The Kafka message value type (e.g., OutboxMessageProto).</typeparam>
internal class KafkaConsumerCore<TKey, TValue> : IDisposable where TValue : class // TValue as class for Protobuf
{
    private readonly ILogger _logger; // Use a generic ILogger, not specific to KafkaConsumerCore
    private readonly ConsumerConfig _consumerConfig;
    private readonly string _topic;
    private IConsumer<TKey, TValue>? _consumer;
    private ISchemaRegistryClient? _schemaRegistryClient; // Only if using schema-based deserialization
    private readonly Func<ISchemaRegistryClient?, IDeserializer<TValue>> _valueDeserializerFactory;
    private bool _isDisposed;
    private readonly string _instanceId; // For logging

    public KafkaConsumerCore(
        ConsumerConfig consumerConfig,
        string topicToSubscribe,
        Func<ISchemaRegistryClient?, IDeserializer<TValue>> valueDeserializerFactory, // e.g., (srClient) => new ProtobufDeserializer<TValue>(srClient).AsSyncOverAsync()
        IDeserializer<TKey> keyDeserializer,
        ILogger logger,
        string instanceId,
        SchemaRegistryConfig? schemaRegistryConfig = null) // Optional: if SR is needed by deserializer factory
    {
        _consumerConfig = consumerConfig ?? throw new ArgumentNullException(nameof(consumerConfig));
        _topic = topicToSubscribe ?? throw new ArgumentNullException(nameof(topicToSubscribe));
        _valueDeserializerFactory = valueDeserializerFactory ?? throw new ArgumentNullException(nameof(valueDeserializerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _instanceId = instanceId;

        if (schemaRegistryConfig != null)
        {
            _schemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);
        }

        InitializeConsumer(keyDeserializer);
    }

    private void InitializeConsumer(IDeserializer<TKey> keyDeserializer)
    {
        try
        {
            IDeserializer<TValue> valueDeserializer = _valueDeserializerFactory(_schemaRegistryClient);

            var builder = new ConsumerBuilder<TKey, TValue>(_consumerConfig)
                .SetKeyDeserializer(keyDeserializer)
                .SetValueDeserializer(valueDeserializer)
                .SetErrorHandler((c, e) => HandleKafkaError(c, e, "CoreConsumer"))
                .SetLogHandler((c, l) => HandleKafkaLog(c, l, "CoreConsumer"))
                .SetStatisticsHandler((c, s) => HandleKafkaStats(c, s, "CoreConsumer"))
                .SetPartitionsAssignedHandler((c, partitions) =>
                {
                    _logger.LogInformation("[KCC-{InstanceId}] Consumer assigned partitions: [{PartitionsInfo}]. Current: [{CurrentAssignments}]",
                        _instanceId,
                        string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]")),
                        string.Join(", ", c.Assignment.Select(p => $"{p.Topic}[{p.Partition}]")));
                })
                .SetPartitionsRevokedHandler((c, partitions) =>
                {
                    _logger.LogWarning("[KCC-{InstanceId}] Consumer revoking partitions: [{PartitionsInfo}]. Current before revoke: [{CurrentAssignments}]",
                        _instanceId,
                        string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]@{p.Offset}")),
                        string.Join(", ", c.Assignment.Select(p => $"{p.Topic}[{p.Partition}]")));
                })
                .SetOffsetsCommittedHandler((c, committedOffsets) => {
                    if (committedOffsets.Error is not null && committedOffsets.Error.Code != ErrorCode.NoError) {
                         _logger.LogError("[KCC-{InstanceId}] Failed to commit offsets: [{ErrorReason}]. Offsets: [{Offsets}]", _instanceId, committedOffsets.Error.Reason, string.Join(", ", committedOffsets.Offsets.Select(o => $"{o.TopicPartition}@{o.Offset}")));
                    } else {
                         _logger.LogDebug("[KCC-{InstanceId}] Successfully committed offsets: [{Offsets}]", _instanceId, string.Join(", ", committedOffsets.Offsets.Select(o => $"{o.TopicPartition}@{o.Offset}")));
                    }
                });

            _consumer = builder.Build();
            _consumer.Subscribe(_topic);
            _logger.LogInformation("[KCC-{InstanceId}] KafkaConsumerCore initialized and subscribed to topic '{Topic}'. GroupId: {GroupId}", _instanceId, _topic, _consumerConfig.GroupId);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[KCC-{InstanceId}] Failed to initialize KafkaConsumerCore for topic '{Topic}', GroupId: {GroupId}.", _instanceId, _topic, _consumerConfig.GroupId);
            throw; // Re-throw to prevent the application from starting in a bad state
        }
    }

    public ConsumeResult<TKey, TValue>? ConsumeRaw(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (_isDisposed || _consumer == null)
        {
            _logger.LogWarning("[KCC-{InstanceId}] ConsumeRaw called but consumer is disposed or not initialized.", _instanceId);
            return null;
        }

        try
        {
            // Consume is a blocking call. It respects the CancellationToken if one is passed to it.
            // However, the Confluent .NET client's Consume(TimeSpan) does not directly accept a CancellationToken.
            // Cancellation is typically handled by checking token before/after or by using Consume(CancellationToken) which blocks indefinitely until message or cancellation.
            // For a timeout-based consume that's also cancellable, we check token before and rely on timeout for unblocking.
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            return _consumer.Consume(timeout);
        }
        catch (ConsumeException ex)
        {
            // Let the error handler (HandleKafkaError) log details.
            // Re-throw if fatal, otherwise the caller (KafkaOutboxMessageProcessor) can decide to retry or log.
            if (ex.Error.IsFatal)
            {
                _logger.LogCritical(ex, "[KCC-{InstanceId}] Fatal ConsumeException in ConsumeRaw. Consumer may be compromised.", _instanceId);
                throw; // Propagate fatal errors
            }
            _logger.LogWarning(ex, "[KCC-{InstanceId}] Non-fatal ConsumeException in ConsumeRaw: {Reason}", _instanceId, ex.Error.Reason);
            return null; // Return null for non-fatal, allowing caller to decide next step
        }
        // OperationCanceledException should be caught by the caller if it passes a CancellationToken that gets triggered
        // during the blocking period of Consume or if checked before.
    }

    public void Commit(ConsumeResult<TKey, TValue> consumeResult)
    {
        if (_isDisposed || _consumer == null)
        {
            _logger.LogWarning("[KCC-{InstanceId}] Commit called but consumer is disposed or not initialized.", _instanceId);
            return;
        }
        if (_consumerConfig.EnableAutoCommit == true) // Should be false for our pattern
        {
            _logger.LogDebug("[KCC-{InstanceId}] Auto-commit is enabled; manual commit call is redundant.", _instanceId);
            return;
        }
        try
        {
            _consumer.Commit(consumeResult);
        }
        catch (KafkaException e)
        {
            _logger.LogError(e, "[KCC-{InstanceId}] KafkaException during explicit commit for offset {Offset}: {Reason}", _instanceId, consumeResult.TopicPartitionOffset, e.Error.Reason);
            if (e.Error.IsFatal) throw; // Re-throw fatal commit errors
            // Non-fatal commit errors are tricky; might lead to reprocessing.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[KCC-{InstanceId}] Unexpected exception during explicit commit for offset {Offset}.", _instanceId, consumeResult.TopicPartitionOffset);
            throw;
        }
    }

    public void StoreOffset(ConsumeResult<TKey, TValue> consumeResult)
    {
        if (_isDisposed || _consumer == null) return;
        try
        {
            // This is useful if you want to batch commits or commit based on external signals.
            // For the outbox relay, direct commit after processing is usually preferred.
            _consumer.StoreOffset(consumeResult);
            _logger.LogDebug("[KCC-{InstanceId}] Stored offset {Offset} for later commit.", _instanceId, consumeResult.TopicPartitionOffset);
        }
        catch (KafkaException e)
        {
             _logger.LogError(e, "[KCC-{InstanceId}] KafkaException during StoreOffset for offset {Offset}: {Reason}", _instanceId, consumeResult.TopicPartitionOffset, e.Error.Reason);
        }
    }


    private void HandleKafkaError(IConsumer<TKey, TValue> consumer, Error error, string consumerNameSuffix)
    {
        LogLevel level = error.IsFatal ? LogLevel.Critical :
                         error.IsBrokerError || error.IsLocalError ? LogLevel.Error : LogLevel.Warning;
        _logger.Log(level, "[KCC-{InstanceId}] Kafka {ConsumerNameSuffix} Error: Code={ErrorCode}, Reason='{Reason}', IsBroker={IsBroker}, IsLocal={IsLocal}, IsFatal={IsFatal}",
            _instanceId, consumerNameSuffix, error.Code, error.Reason, error.IsBrokerError, error.IsLocalError, error.IsFatal);
        if (error.IsFatal)
        {
            _logger.LogCritical("[KCC-{InstanceId}] Fatal Kafka error in {ConsumerNameSuffix}. Consumer is likely compromised.", _instanceId, consumerNameSuffix);
            // Consider a mechanism to signal fatal error to the owner of this KafkaConsumerCore instance.
        }
    }

    private void HandleKafkaLog(IConsumer<TKey, TValue> consumer, LogMessage logMessage, string consumerNameSuffix)
    {
        LogLevel logLevel = logMessage.Level.ToLogLevel(); // Using your extension method
        _logger.Log(logLevel, "[KCC-{InstanceId}] Kafka {ConsumerNameSuffix} Client Log [{Source} Lvl {LevelNum} Fac {Facility}]: {Message}",
            _instanceId, consumerNameSuffix, logMessage.Name, (int)logMessage.Level, logMessage.Facility, logMessage.Message);
    }

    private void HandleKafkaStats(IConsumer<TKey, TValue> consumer, string jsonStats, string consumerNameSuffix)
    {
        _logger.LogInformation("[KCC-{InstanceId}] Kafka {ConsumerNameSuffix} Statistics: {StatsJson}", _instanceId, consumerNameSuffix, jsonStats);
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
            _logger.LogInformation("[KCC-{InstanceId}] Disposing KafkaConsumerCore for topic '{Topic}', GroupId: {GroupId}.", _instanceId, _topic, _consumerConfig.GroupId);
            if (_consumer != null)
            {
                try
                {
                    // Unsubscribe may not be strictly necessary if Close is called, but doesn't hurt.
                    // _consumer.Unsubscribe();
                    _consumer.Close(); // Gracefully leave the consumer group and commit final offsets if auto-commit was on.
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[KCC-{InstanceId}] Exception during Kafka consumer Close/Unsubscribe.", _instanceId);
                }
                finally
                {
                    _consumer.Dispose();
                    _consumer = null;
                }
            }
            _schemaRegistryClient?.Dispose();
            _schemaRegistryClient = null;
             _logger.LogInformation("[KCC-{InstanceId}] KafkaConsumerCore disposed.", _instanceId);
        }
        _isDisposed = true;
    }
}

// Keep your KafkaLogLevelExtensions here or in a shared utility file
internal static class KafkaLogLevelExtensions
{
    internal static LogLevel ToLogLevel(this SyslogLevel syslogLevel) => syslogLevel switch
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
