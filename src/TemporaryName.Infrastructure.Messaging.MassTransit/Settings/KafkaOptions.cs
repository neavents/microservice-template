// src/TemporaryName.Infrastructure.Messaging.MassTransit/Settings/KafkaOptions.cs
using Confluent.Kafka; // For SaslMechanism, SecurityProtocol, etc.
using System.Collections.Generic;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

public class KafkaSecurityOptions
{
    public SecurityProtocol? SecurityProtocol { get; set; } // None, Ssl, SaslPlaintext, SaslSsl
    public SaslMechanism? SaslMechanism { get; set; } // Gssapi, Plain, ScramSha256, ScramSha512, OAuthBearer

    // SASL PLAIN/SCRAM credentials
    public string? SaslUsername { get; set; }
    public string? SaslPassword { get; set; }

    // SASL OAuthBearer
    public string? SaslOauthbearerConfig { get; set; } // See Confluent.Kafka documentation

    // SSL/TLS Settings
    public string? SslCaLocation { get; set; }
    public string? SslCertificateLocation { get; set; }
    public string? SslKeyLocation { get; set; }
    public string? SslKeyPassword { get; set; }
    public string? SslCipherSuites { get; set; }
    public string? SslCurvesList { get; set; }
    public string? SslSigalgsList { get; set; }
    public bool? EnableSslCertificateVerification { get; set; } = true;
    public SslEndpointIdentificationAlgorithm? SslEndpointIdentificationAlgorithm { get; set; } // Https, None
}

public class KafkaTopicOptions
{
    /// <summary>
    /// Name of the topic. Can include placeholders like {MessageType} which MassTransit replaces.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Number of partitions for the topic. -1 to use broker default.
    /// </summary>
    public int NumPartitions { get; set; } = -1;

    /// <summary>
    /// Replication factor for the topic. -1 to use broker default.
    /// </summary>
    public short ReplicationFactor { get; set; } = -1; // Should be >= 1, typically 3 in production

    /// <summary>
    /// Custom topic configurations (e.g., "retention.ms", "cleanup.policy").
    /// </summary>
    public Dictionary<string, string>? Configs { get; set; }

    /// <summary>
    /// If true, MassTransit will attempt to create the topic if it doesn't exist.
    /// Requires broker-side `auto.create.topics.enable=true` (if not using AdminClient) or appropriate ACLs.
    /// </summary>
    public bool AutoCreate { get; set; } = true; // Be cautious in production
}

public class KafkaProducerOptions
{
    public Acks? Acks { get; set; } = Confluent.Kafka.Acks.All; // Default for high reliability
    public int? MessageSendMaxRetries { get; set; } = 2;
    public int? RetryBackoffMs { get; set; } = 100;
    public CompressionType? CompressionType { get; set; } = Confluent.Kafka.CompressionType.Snappy; // Good balance
    public int? LingerMs { get; set; } = 5; // Time to buffer messages before sending
    public int? BatchSize { get; set; } // In bytes
    public bool? EnableIdempotence { get; set; } = true; // Recommended for exactly-once (within producer)
    // Other producer-specific librdkafka settings can be added here
}

public class KafkaConsumerGroupOptions
{
    /// <summary>
    /// The consumer group ID.
    /// </summary>
    public required string GroupId { get; set; }

    /// <summary>
    /// Topics this consumer group will subscribe to.
    /// </summary>
    public List<string> Topics { get; set; } = new();


    /// <summary>
    /// How to handle offsets when initially connecting if no offset is stored.
    /// </summary>
    public AutoOffsetReset? AutoOffsetReset { get; set; } = Confluent.Kafka.AutoOffsetReset.Latest;

    /// <summary>
    /// If true, offsets are committed automatically by the Kafka client library.
    /// Set to false for manual commits after processing (more control). MassTransit typically manages this.
    /// </summary>
    public bool? EnableAutoCommit { get; set; } = false; // MassTransit prefers to control commits
    public int? AutoCommitIntervalMs { get; set; } = 5000;

    public int? SessionTimeoutMs { get; set; } = 45000; // librdkafka default is 45s
    public int? MaxPollIntervalMs { get; set; } = 300000; // Time allowed for processing before rebalance
    public int? FetchMinBytes { get; set; } = 1;
    public int? FetchMaxWaitMs { get; set; } = 500;

    /// <summary>
    /// Number of concurrent message listeners per partition. Default is 1.
    /// Increasing this can improve throughput but complicates ordering if not careful.
    /// </summary>
    public int ConcurrencyLimit { get; set; } = 1;

    /// <summary>
    /// Max number of (partition)Eof events to ignore before raising an error. Useful for idle detection.
    /// </summary>
    public int? MaxEofsToIgnore { get; set; }
}


public class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string? ClientIdPrefix { get; set; } // Prepended to MassTransit's generated client.id

    public KafkaSecurityOptions Security { get; set; } = new();

    /// <summary>
    /// Default producer settings for messages published via MassTransit to Kafka.
    /// </summary>
    public KafkaProducerOptions DefaultProducer { get; set; } = new();

    /// <summary>
    /// Configuration for consumer groups. Keyed by a logical name for the group configuration.
    /// MassTransit will use these when configuring TopicEndpoints.
    /// </summary>
    public Dictionary<string, KafkaConsumerGroupOptions>? ConsumerGroups { get; set; }

    /// <summary>
    /// Defines topics that MassTransit might interact with (e.g., for publishing or ensuring they exist).
    /// Keyed by a logical name or message type name.
    /// </summary>
    public Dictionary<string, KafkaTopicOptions>? Topics { get; set; }


    // --- MassTransit Outbox Pattern (for sends originating from this service) ---
    public bool UseInMemoryOutbox { get; set; } = false;
    public bool UseEntityFrameworkCoreOutbox { get; set; } = false;
    public string? EntityFrameworkCoreOutboxDbContextTypeFullName { get; set; }
    public int EntityFrameworkCoreOutboxQueryDelayMs { get; set; } = 1000;
    public int EntityFrameworkCoreOutboxQueryMessageLimit { get; set; } = 100;

    /// <summary>
    /// Retry policies for consumers on this Kafka transport.
    /// </summary>
    public ConsumerRetryOptions ConsumerRetry { get; set; } = new();

    /// <summary>
    /// Dead letter strategy for this Kafka transport (typically forwarding to a DLT).
    /// </summary>
    public DeadLetterStrategyOptions DeadLetterStrategy { get; set; } = new();

    /// <summary>
    /// Optional: Custom name for the bus instance.
    /// </summary>
    public string? BusInstanceName { get; set; }

    /// <summary>
    /// Timeout for AdminClient operations (like topic creation) in milliseconds.
    /// </summary>
    public int AdminClientTimeoutMs { get; set; } = 5000;
}