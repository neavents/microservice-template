// src/TemporaryName.Infrastructure.Messaging.MassTransit/Settings/MassTransitOptions.cs
namespace TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

public class MassTransitOptions
{
    public const string SectionName = "MassTransit";

    /// <summary>
    /// Specifies the primary message broker type to use.
    /// Supported: "RabbitMQ", "Kafka".
    /// </summary>
    public string MessageBrokerType { get; set; } = "RabbitMQ";

    /// <summary>
    /// Optional: Name for this service, used in endpoint naming and client IDs.
    /// If not set, assembly name might be used.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Global prefetch count if not specified at transport level.
    /// More relevant for RabbitMQ.
    /// </summary>
    public ushort? GlobalPrefetchCount { get; set; }

    /// <summary>
    /// Configuration for RabbitMQ transport.
    /// Used if MessageBrokerType is "RabbitMQ".
    /// </summary>
    public RabbitMqOptions RabbitMq { get; set; } = new();

    /// <summary>
    /// Configuration for Kafka transport.
    /// Used if MessageBrokerType is "Kafka".
    /// </summary>
    public KafkaOptions Kafka { get; set; } = new();

    /// <summary>
    /// Enables detailed logging of MassTransit operations to ILogger.
    /// Options: "All", "Debug", "Info", "Warn", "Error", "None"
    /// </summary>
    public string LogLevel { get; set; } = "Info"; // Controls MassTransit's own logging verbosity

    /// <summary>
    /// If true, enables OpenTelemetry tracing for MassTransit.
    /// </summary>
    public bool EnableOpenTelemetry { get; set; } = true;

    /// <summary>
    /// Settings for how entity (message type) names are formatted for topics/exchanges.
    /// Options: "KebabCase", "PascalCase", "SnakeCase", "Default" (usually PascalCase)
    /// </summary>
    public string EntityNameFormatter { get; set; } = "KebabCase";

    /// <summary>
    /// Timeout for bus operations like request-response in milliseconds.
    /// </summary>
    public int DefaultTimeoutMs { get; set; } = 30000; // 30 seconds

    /// <summary>
    /// Settings for message schedulers if external schedulers are used (e.g., Quartz, Hangfire with MassTransit integration).
    /// The built-in delayed exchange (RabbitMQ) or producer-side delay (Kafka, less common for long delays) are often preferred.
    /// </summary>
    // public SchedulerOptions Scheduler { get; set; } = new();
}

// public class SchedulerOptions
// {
//    public bool UseExternalScheduler { get; set; } = false;
//    public string? SchedulerQueueName { get; set; } // e.g., "quartz-scheduler"
// }