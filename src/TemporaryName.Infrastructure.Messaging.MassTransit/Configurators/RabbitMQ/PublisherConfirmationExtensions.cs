using System;
using MassTransit;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators.RabbitMQ;

public static class PublisherConfirmationExtensions
    {
        /// <summary>
        /// Configures publisher acknowledgements (confirms) for messages sent to RabbitMQ.
        /// MassTransit enables this by default for RabbitMQ, but this makes it explicit.
        /// It also configures a timeout for waiting for these confirmations.
        /// </summary>
        public static IRabbitMqBusFactoryConfigurator ConfigurePublisherConfirmations(
            this IRabbitMqBusFactoryConfigurator configurator,
            TimeSpan? confirmationTimeout = null)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            
        configurator.ConfigurePublisherConfirmations(confirmationTimeout ?? TimeSpan.FromSeconds(30));

        return configurator;
        }
    }