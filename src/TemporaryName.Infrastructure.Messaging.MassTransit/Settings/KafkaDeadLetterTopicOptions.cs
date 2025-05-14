using System;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

public class KafkaDeadLetterTopicOptions
{
    /// <summary>
    /// Name of the Dead Letter Topic (DLT) to forward unprocessable messages.
    /// Example: "myapp-service-errors" or "{OriginalTopic}-dlq"
    /// </summary>
    public string DeadLetterTopicNameFormat { get; set; } = "{OriginalTopic}_error"; // Placeholder for original topic name

    /// <summary>
    /// If true, attempts to create the dead letter topic if it doesn't exist.
    /// Requires appropriate Kafka broker permissions.
    /// </summary>
    public bool CreateDeadLetterTopic { get; set; } = true;

    /// <summary>
    /// Number of partitions for the auto-created dead letter topic.
    /// </summary>
    public int DeadLetterTopicPartitions { get; set; } = 1;

    /// <summary>
    /// Replication factor for the auto-created dead letter topic.
    /// </summary>
    public short DeadLetterTopicReplicationFactor { get; set; } = 1; // Adjust for production

    /// <summary>
    /// If true, adds diagnostic headers (e.g., exception message, stack trace, original topic) to the message sent to DLT.
    /// </summary>
    public bool AddDiagnosticHeaders { get; set; } = true;
}