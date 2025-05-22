using System;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

public class EndpointConsumerTimeoutOptions
{
public const string SectionName = "RabbitMqEndpointConsumerTimeout";

        /// <summary>
        /// Enables or disables consumer execution timeouts for the endpoint.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The maximum duration a consumer's Consume method is allowed to run.
        /// If exceeded, a ConsumerTimeoutException is thrown, and the message
        /// will typically be retried and eventually moved to the error queue.
        /// Example: "00:00:30" for 30 seconds.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
