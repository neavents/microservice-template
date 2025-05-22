using System;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

public class EndpointCircuitBreakerOptions
{
        public const string SectionName = "RabbitMqEndpointCircuitBreaker";

        /// <summary>
        /// Enables or disables the circuit breaker for the endpoint.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The duration of time the circuit breaker remains in the half-open state,
        /// allowing a single message to pass through to test the consumer.
        /// Default: 1 minute.
        /// </summary>
        public TimeSpan TrackingPeriod { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The number of consecutive failures required to trip the circuit breaker,
        /// moving it to the open state.
        /// Default: 15.
        /// </summary>
        public int TripThreshold { get; set; } = 15; // Percentage (e.g., 15 = 15% failure rate over tracking period)

        /// <summary>
        /// The number of messages that must pass through the circuit breaker
        /// during the TrackingPeriod before it calculates the failure rate.
        /// This prevents tripping on startup with few messages.
        /// Default: 10.
        /// </summary>
        public int ActiveThreshold { get; set; } = 10; // Minimum messages before tripping

        /// <summary>
        /// The duration of time the circuit breaker remains in the open state,
        /// after which it transitions to the half-open state.
        /// Default: 5 minutes.
        /// </summary>
        public TimeSpan ResetInterval { get; set; } = TimeSpan.FromMinutes(5);
}
