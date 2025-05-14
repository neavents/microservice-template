namespace TemporaryName.Infrastructure.Outbox.EFCore.Settings;

/// <summary>
/// Settings for the OutboxEventRelayService.
/// These settings control the behavior of the relay service itself,
/// not the underlying message source (e.g., Kafka consumer settings are separate).
/// </summary>
public class OutboxEventRelaySettings
{
    public const string SectionName = "OutboxEventRelay";

    /// <summary>
    /// Enables or disables the OutboxEventRelayService.
    /// If false, the background service will not start its processing loop.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// A logical name for this instance of the relay service, primarily used for logging.
    /// This helps differentiate logs if multiple relay instances were ever run (though typically one per outbox source).
    /// The Kafka Consumer Group ID is configured separately in KafkaOutboxConsumerSettings.
    /// </summary>
    public string RelayInstanceLogName { get; set; } = "OutboxRelayInstance-01";


    /// <summary>
    /// Timeout in milliseconds for polling messages from the IOutboxMessageSource.
    /// This determines how frequently the relay service checks for new messages if the source is empty.
    /// </summary>
    public int ConsumeTimeoutMs { get; set; } = 1000; // e.g., 1 second

    /// <summary>
    /// Maximum number of retry attempts for publishing an event via IIntegrationEventPublisher
    /// when transient errors occur.
    /// </summary>
    public int HandlerMaxRetryAttempts { get; set; } = 5;

    /// <summary>
    /// Base delay in seconds for the exponential backoff retry strategy used by Polly.
    /// The actual delay will be calculated as BaseDelay^retryAttempt.
    /// </summary>
    public double HandlerRetryBaseDelaySeconds { get; set; } = 2.0;
}