using System;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

    public class EndpointConcurrencyOptions
    {
        public const string SectionName = "RabbitMqEndpointConcurrency";

        /// <summary>
        /// Gets or sets the maximum number of concurrent messages that can be consumed by the endpoint.
        /// Default is null, which means MassTransit will calculate a sensible default (often based on processor count).
        /// Setting this too high can lead to resource exhaustion if message processing is CPU/IO bound.
        /// Setting this too low can limit throughput.
        /// </summary>
        public int? ConcurrentMessageLimit { get; set; } // Example: 16

        /// <summary>
        /// Gets or sets the prefetch count for the endpoint.
        /// This is the number of messages the broker will send to the consumer before waiting for acknowledgments.
        /// It should generally be equal to or slightly higher than the ConcurrentMessageLimit.
        /// MassTransit default is usually calculated based on ConcurrentMessageLimit.
        /// </summary>
        public ushort? PrefetchCount { get; set; } // Example: 20

        // Potentially add settings for specific consumer types if you want very granular global defaults
        // public Dictionary<string, ConsumerConcurrencySettings> ConsumerSpecificLimits { get; set; } = new();
    }
