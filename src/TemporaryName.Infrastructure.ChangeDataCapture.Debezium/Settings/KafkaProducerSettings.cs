using Confluent.Kafka;

namespace TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Settings;

public class KafkaProducerSettings // Simplified for DLQ, could be more generic
{
    public const string DefaultSectionName = "Infrastructure:KafkaDebeziumDlqProducer";

    public required string BootstrapServers { get; set; }
    public string ClientId { get; set; } = "generic-dlq-producer";
    public string Acks { get; set; } = "All"; // Leader, All, None

    public string? SchemaRegistryUrl { get; set; }
    public string? SchemaRegistryBasicAuthUserInfo { get; set; }

    public SecurityProtocol? SecurityProtocol { get; set; }
    public SaslMechanism? SaslMechanism { get; set; }
    public string? SaslUsername { get; set; }
    public string? SaslPassword { get; set; }
    public string? SslCaLocation { get; set; }
    public string? SslCertificateLocation { get; set; }
    public string? SslKeyLocation { get; set; }
    public string? SslKeyPassword { get; set; }
}