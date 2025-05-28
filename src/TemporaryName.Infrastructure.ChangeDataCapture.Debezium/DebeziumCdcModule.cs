using Autofac;
using Confluent.SchemaRegistry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Abstractions;
using TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Services;
using TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Settings;

namespace TemporaryName.Infrastructure.ChangeDataCapture.Debezium;

public class DebeziumCdcModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Schema Registry Client (singleton for caching)
        builder.Register(c =>
        {
            // This assumes KafkaConsumerSettings contains the SR URL.
            // A more generic ISchemaRegistrySettings could be used.
            // For now, let's assume the consumer settings are primary source for SR config.
            // If multiple consumers have different SRs (unlikely), named options needed.
            var consumerSettings = c.Resolve<IOptions<KafkaConsumerSettings>>().Value; // Gets default unnamed options
            if (string.IsNullOrEmpty(consumerSettings.SchemaRegistryUrl))
            {
                // Log or throw if SR is essential but not configured
                // For simplicity, let it be null if not configured, consumer will handle.
                return null;
            }
            var schemaRegistryConfig = new SchemaRegistryConfig { Url = consumerSettings.SchemaRegistryUrl };
            if (!string.IsNullOrEmpty(consumerSettings.SchemaRegistryBasicAuthUserInfo))
            {
                schemaRegistryConfig.BasicAuthUserInfo = consumerSettings.SchemaRegistryBasicAuthUserInfo;
            }
            return new CachedSchemaRegistryClient(schemaRegistryConfig);
        })
        .As<ISchemaRegistryClient>()
        .SingleInstance()
        .IfNotRegistered(typeof(ISchemaRegistryClient)); // Register only if not already there


        // Register DLQ Producer factory/instance if needed by consumers
        // This uses named options for KafkaProducerSettings, keyed by a "producerName"
        // The GenericDebeziumConsumer doesn't directly use this, but an injected service might.
        // For the DLQ producer used *within* GenericDebeziumConsumer, it's created internally
        // based on its own KafkaConsumerSettings (for BootstrapServers) and a simplified DLQ producer config.

        // To provide a general purpose IKafkaProducer (e.g., for DLQ used by GenericDebeziumConsumer),
        // we need to decide if it uses the same BootstrapServers as the consumer or has its own config.
        // Let's assume DLQ producer uses settings from a "DlqProducer" named configuration.
        builder.Register(c =>
        {
            var producerSettingsMonitor = c.Resolve<IOptionsMonitor<KafkaProducerSettings>>();
            var logger = c.Resolve<ILogger<KafkaGenericProducer<byte[], byte[]>>>(); // Example using byte[] for generic DLQ
            ISchemaRegistryClient? srClient = c.ResolveOptional<ISchemaRegistryClient>();
            // "DefaultDlqProducer" is the name for the IOptionsMonitor<KafkaProducerSettings>
            return new KafkaGenericProducer<byte[], byte[]>(producerSettingsMonitor, "DefaultDlqProducer", logger, srClient);
        })
        .Named<IKafkaProducer<byte[], byte[]>>("DefaultDlqByteArrayProducer") // Named for specific use
        .InstancePerLifetimeScope(); // Or Singleton if config doesn't change

        // Registration of GenericDebeziumConsumer<,,> is handled by IServiceCollection.AddHostedService
        // in the DependencyInjection.cs file for this project.
        // Specific IDebeziumEventHandler<,> implementations are registered by the application/host project.
    }
}