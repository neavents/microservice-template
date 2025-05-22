using System;
using MassTransit;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators.RabbitMQ;

public static class MessageSchedulingExtensions
    {
        /// <summary>
        /// Configures the bus to use RabbitMQ's delayed exchange plugin for message scheduling.
        /// This is required if you use `_bus.ScheduleSend(...)` or sagas with scheduled events.
        /// The RabbitMQ server must have the `rabbitmq_delayed_message_exchange` plugin enabled.
        /// </summary>
        public static IRabbitMqBusFactoryConfigurator ConfigureRabbitMqMessageScheduler(
            this IRabbitMqBusFactoryConfigurator configurator)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            
            configurator.ConfigureRabbitMqMessageScheduler();

            return configurator;
        }
    }
