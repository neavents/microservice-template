using System.ComponentModel.DataAnnotations;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

public class MassTransitOptions
{
    public const string SectionName = "MassTransit";

    [Required(AllowEmptyStrings = false, ErrorMessage = "MessageBrokerType is required and cannot be empty. Supported: RabbitMQ, Kafka.")]
    [RegularExpression("^(RabbitMQ|Kafka)$", ErrorMessage = "MessageBrokerType must be either 'RabbitMQ' or 'Kafka'.")]
    public string MessageBrokerType { get; set; } = "RabbitMQ";

    [StringLength(100, MinimumLength = 3, ErrorMessage = "ServiceName must be between 3 and 100 characters if provided.")]
    public string? ServiceName { get; set; } 

    [Range(1, 1024, ErrorMessage = "GlobalPrefetchCount must be between 1 and 1024 if provided.")]
    public ushort? GlobalPrefetchCount { get; set; }

    [RegularExpression("^(All|Debug|Information|Warning|Error|None)$", ErrorMessage = "LogLevel must be one of: All, Debug, Information, Warning, Error, None.")]
    public string LogLevel { get; set; } = "Information";

    public bool EnableOpenTelemetry { get; set; } = true;

    [RegularExpression("^(KebabCase|PascalCase|SnakeCase|Default)$", ErrorMessage = "EntityNameFormatter must be one of: KebabCase, PascalCase, SnakeCase, Default.")]
    public string EntityNameFormatter { get; set; } = "KebabCase";

    [Range(1000, 300000, ErrorMessage = "DefaultTimeoutMs for bus operations must be between 1000 (1s) and 300000 (5min).")]
    public int DefaultTimeoutMs { get; set; } = 30000; 
}