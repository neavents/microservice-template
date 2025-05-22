using System;
using MassTransit;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators.RabbitMQ;

public static class ProtobufSerializationExtensions
    {
        /// <summary>
        /// Configures MassTransit to use Protobuf for message serialization and deserialization.
        /// </summary>
        public static IRabbitMqBusFactoryConfigurator ConfigureProtobufSerialization(
            this IRabbitMqBusFactoryConfigurator configurator)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            
            configurator.ConfigureProtobufSerialization();

            return configurator;
        }
    }
