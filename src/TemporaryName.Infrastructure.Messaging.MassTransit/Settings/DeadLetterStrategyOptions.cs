// src/TemporaryName.Infrastructure.Messaging.MassTransit/Settings/DeadLetterStrategyOptions.cs
namespace TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

public class DeadLetterStrategyOptions
{
    /// <summary>
    /// Enables or disables the dead-lettering mechanism. Default is true.
    /// If false, terminally failing messages might be discarded or block queues, depending on broker.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Options specific to RabbitMQ dead-lettering.
    /// </summary>
    public RabbitMqDeadLetterOptions RabbitMq { get; set; } = new();

    /// <summary>
    /// Options specific to Kafka dead-lettering (typically forwarding to a Dead Letter Topic).
    /// </summary>
    public KafkaDeadLetterTopicOptions Kafka { get; set; } = new();
}