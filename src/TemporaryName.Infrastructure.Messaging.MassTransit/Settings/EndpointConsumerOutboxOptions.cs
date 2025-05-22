using System;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

public class EndpointConsumerOutboxOptions
{
        public const string SectionName = "RabbitMqEndpointConsumerOutbox";

        /// <summary>
        /// Enables or disables the EF Core Transactional Outbox (Inbox pattern) for the endpoint.
        /// When enabled, it ensures exactly-once processing semantics for consumers
        /// that interact with the database.
        /// Requires YourDbContext to be registered and the producer-side outbox to be configured.
        /// </summary>
        public bool Enabled { get; set; } = true; // Recommended to be true for consumers interacting with DB
}
