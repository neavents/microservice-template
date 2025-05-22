using System;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

public class EndpointRateLimiterOptions
{
        public const string SectionName = "RabbitMqEndpointRateLimiter";

        /// <summary>
        /// Enables or disables the rate limiter for the endpoint.
        /// </summary>
        public bool Enabled { get; set; } = false; // Disabled by default, enable explicitly

        /// <summary>
        /// The maximum number of messages that can be processed per <see cref="RouterMode"/> (e.g., per second).
        /// This is the "rate limit".
        /// </summary>
        public int MessageLimit { get; set; } = 100;

        /// <summary>
        /// The time interval for the message limit.
        /// Example: If MessageLimit is 100 and Interval is 1 second, the limit is 100 messages/second.
        /// </summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1);
}
