// src/TemporaryName.Infrastructure.ChangeDataCapture.Debezium/Settings/KafkaOutboxConsumerSettings.cs
using Confluent.Kafka; // For AutoOffsetReset, SaslMechanism, SecurityProtocol
using System.Collections.Generic;

namespace TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Settings;

/// <summary>
/// Settings for the Kafka consumer that reads Protobuf-encoded outbox messages
/// published by Debezium to a Kafka topic. This consumer is the source for the OutboxEventRelayService.
/// </summary>
public class KafkaOutboxConsumerSettings
{
    public const string SectionName = "KafkaOutboxConsumer"; // Matches appsettings.json section

    /// <summary>
    /// Enables or disables this Kafka consumer source.
    /// If false, the KafkaOutboxMessageSource will not attempt to connect or consume.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Comma-separated list of Kafka broker addresses (host:port).
    /// Example: "kafka1:9092,kafka2:9092"
    /// </summary>
    public required string BootstrapServers { get; set; }

    /// <summary>
    /// The Kafka consumer group ID. This is CRITICAL for the outbox relay.
    /// All instances of the OutboxEventRelayService that should share the workload
    /// of processing outbox messages must use the SAME GroupId.
    /// Ensures each message is processed by only one consumer instance within the group.
    /// Example: "outbox-event-relay-processor-v1"
    /// </summary>
    public required string GroupId { get; set; }

    /// <summary>
    /// The Kafka topic from which Debezium publishes the outbox messages.
    /// Example: "dbserver1.public.outbox_messages" (Debezium's default naming convention)
    /// </summary>
    public required string OutboxKafkaTopic { get; set; }

    /// <summary>
    /// Action to take when there is no initial offset in Kafka or if the current offset does not exist anymore on the server.
    /// Default is Earliest to process all available messages if the consumer group is new.
    /// Latest: starts consuming from the latest messages.
    /// Error: throws an exception.
    /// </summary>
    public AutoOffsetReset AutoOffsetResetEnum { get; set; } = AutoOffsetReset.Earliest;

    /// <summary>
    /// IMPORTANT: Must be false for the transactional outbox relay pattern to work correctly with manual commits.
    /// Offsets are committed manually by the KafkaOutboxMessageSource via AcknowledgeAsync/FailAsync delegates
    /// after the OutboxEventRelayService successfully processes (or definitively fails) the message.
    /// </summary>
    public bool EnableAutoCommit { get; set; } = false;

    /// <summary>
    /// The frequency in milliseconds that the consumer offsets are committed (written) to the broker.
    /// Only relevant if EnableAutoCommit is true. Ignored if false.
    /// </summary>
    public int? AutoCommitIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Client group session and failure detection timeout in milliseconds.
    /// If a consumer fails to send a heartbeat within this window, it's considered dead, and a rebalance occurs.
    /// Default: 45000ms (librdkafka default).
    /// </summary>
    public int? SessionTimeoutMs { get; set; } = 45000;

    /// <summary>
    /// The maximum delay between invocations of the Consume loop. If exceeded, the consumer is considered failed,
    /// and the group will rebalance. This setting limits the time your message processing logic can take.
    /// Default: 300000ms (librdkafka default).
    /// </summary>
    public int? MaxPollIntervalMs { get; set; } = 300000;

    /// <summary>
    /// Minimum number of bytes the broker should wait to accumulate before answering a fetch request.
    /// Default: 1.
    /// </summary>
    public int? FetchMinBytes { get; set; } = 1;

    /// <summary>
    /// Maximum time the broker will wait for FetchMinBytes to be met before answering a fetch request.
    /// Default: 500ms.
    /// </summary>
    public int? FetchMaxWaitMs { get; set; } = 500;

    /// <summary>
    /// Maximum number of messages to return in a single Consume call if `ConsumeBatchSize` is not set.
    /// This is a client-side batching hint, not a strict server-side limit for a single poll.
    /// Default: 500 (librdkafka default for `queued.max.messages.kbytes` influences this too).
    /// </summary>
    // public int? MaxPollRecords { get; set; } = 500; // This is more of a Kafka Streams concept.
                                                 // For Confluent .NET client, batching is more implicit.

    /// <summary>
    /// A unique identifier for this client. If not set, a random one is generated.
    /// Can be useful for correlating logs on the Kafka broker side.
    /// </summary>
    public string? ClientId { get; set; }


    // --- Schema Registry Settings for Protobuf Deserialization ---
    /// <summary>
    /// URL of the Confluent Schema Registry.
    /// Required if Debezium is configured to use ProtobufConverter with schema registration.
    /// Example: "http://localhost:8081"
    /// </summary>
    public string? SchemaRegistryUrl { get; set; }

    /// <summary>
    /// Basic authentication user info for Schema Registry in "user:password" format.
    /// </summary>
    public string? SchemaRegistryBasicAuthUserInfo { get; set; } // "username:password"

    /// <summary>
    /// Initial capacity of the Schema Registry client cache.
    /// Default: 1000.
    /// </summary>
    public int? SchemaRegistryMaxCachedSchemas { get; set; }


    // --- Standard Kafka Security Settings (mirroring Confluent.Kafka.ConsumerConfig) ---
    /// <summary>
    /// Protocol used to communicate with brokers.
    /// Options: Plaintext, Ssl, SaslPlaintext, SaslSsl.
    /// </summary>
    public SecurityProtocol? SecurityProtocol { get; set; }

    /// <summary>
    /// SASL mechanism to use for authentication.
    /// Options: Gssapi, Plain, ScramSha256, ScramSha512, OAuthBearer.
    /// </summary>
    public SaslMechanism? SaslMechanism { get; set; }

    public string? SaslUsername { get; set; }
    public string? SaslPassword { get; set; }
    public string? SaslOauthbearerConfig { get; set; } // For OAuthBearer, complex configuration string

    // SSL/TLS Settings
    public string? SslCaLocation { get; set; } // Path to CA certificate file for verifying broker's certificate.
    public string? SslCertificateLocation { get; set; } // Path to client's certificate.
    public string? SslKeyLocation { get; set; } // Path to client's private key.
    public string? SslKeyPassword { get; set; } // Password for client's private key.
    public string? SslCipherSuites { get; set; } // Comma-separated list of ciphers
    public string? SslCurvesList { get; set; } // Comma-separated list of curves
    public string? SslSigalgsList { get; set; } // Comma-separated list of signature algorithms
    public bool? EnableSslCertificateVerification { get; set; } = true; // Broker certificate verification
    public SslEndpointIdentificationAlgorithm? SslEndpointIdentificationAlgorithm { get; set; } // Https (recommended), None

    /// <summary>
    /// Log connection close events at this level. Default is Debug.
    /// </summary>
    public SyslogLevel? LogConnectionClose { get; set; }

    /// <summary>
    /// Log thread exits at this level. Default is Debug.
    /// </summary>
    public SyslogLevel? LogThreadExit { get; set; }

    /// <summary>
    /// A comma-separated list of debug contexts to enable.
    /// E.g., "all", "consumer,cgrp,topic,fetch".
    /// </summary>
    public string? Debug { get; set; }

    /// <summary>
    /// Allows for additional librdkafka configuration properties not explicitly defined above.
    /// Key-value pairs. Use with caution.
    /// </summary>
    public Dictionary<string, string>? CustomConfiguration { get; set; }
}
