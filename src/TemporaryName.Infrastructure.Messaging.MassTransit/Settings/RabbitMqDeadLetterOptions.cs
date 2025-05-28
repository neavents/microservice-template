using System;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

public class RabbitMqDeadLetterOptions
{
    /// <summary>
    /// If true, uses a convention-based dead-letter exchange (e.g., "DLX_YourExchangeName"). Default is true.
    /// MassTransit often handles this via _error queues tied to the main queue's DLX arguments.
    /// This option provides more global control if needed.
    /// </summary>
    public bool UseConventionDeadLetterExchange { get; set; } = true;

    /// <summary>
    /// Optional: Explicit name for a centralized dead-letter exchange.
    /// If set, messages might be routed here instead of endpoint-specific _error queues directly.
    /// </summary>
    public string? CentralizedDeadLetterExchangeName { get; set; }

    /// <summary>
    /// Optional: Routing key to use when publishing to the CentralizedDeadLetterExchangeName.
    /// Can use placeholders like {OriginalExchange}, {OriginalRoutingKey}, {ExceptionType}.
    /// </summary>
    public string? CentralizedDeadLetterRoutingKey { get; set; } // e.g., "errors.{OriginalExchange}"

    /// <summary>
    /// Time-To-Live (TTL) for messages in the error queue before they are discarded or moved again (if further DLX configured).
    /// In milliseconds. Null or 0 means no TTL.
    /// </summary>
    public int? ErrorQueueTtlMs { get; set; }
}
