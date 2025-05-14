namespace TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Settings;

public class KafkaConsumerSettings
{
    public const string DefaultSectionName = "Infrastructure:KafkaDebeziumConsumer";        

    public required string BootstrapServers { get; set; }
    public required string GroupId { get; set; }
    public required string[] TopicNames { get; set; }
    public string AutoOffsetReset { get; set; } = "Earliest";
    public bool EnableAutoCommit { get; set; }
    public int ConsumeTimeoutMs { get; set; } = 1000;
    public int MaxPollIntervalMs { get; set; } = 300000;

    // Schema Registry
    public required string SchemaRegistryUrl { get; set; }
    public string? SchemaRegistryBasicAuthUserInfo { get; set; } // "user:password"

    // DLQ
    public bool DlqEnabled { get; set; } = true;
    public string? DlqTopicSuffix { get; set; } = ".dlq";
    public string? DlqProducerClientId { get; set; } = "debezium-consumer-dlq-producer";

    // Polly
    public int HandlerMaxRetryAttempts { get; set; } = 3;
    public int HandlerRetryBaseDelaySeconds { get; set; } = 2; // Base for exponential backoff

    public string? SecurityProtocol { get; set; }
    public string? SaslMechanism { get; set; }
    public string? SaslUsername { get; set; }
    public string? SaslPassword { get; set; }
    public string? SslCaLocation { get; set; }
    public string? SslCertificateLocation { get; set; }
    public string? SslKeyLocation { get; set; }
    public string? SslKeyPassword { get; set; }


    public Confluent.Kafka.AutoOffsetReset AutoOffsetResetEnum =>
        Enum.TryParse<Confluent.Kafka.AutoOffsetReset>(AutoOffsetReset, true, out var result)
            ? result
            : Confluent.Kafka.AutoOffsetReset.Earliest;
}
