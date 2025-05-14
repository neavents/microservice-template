using MassTransit;
using MassTransit.Configuration; // For IEntityNameFormatter
using MassTransit.Logging;
using MassTransit.Middleware; // For UseKillSwitch, etc.
using MassTransit.RabbitMqTransport; // For RabbitMQ specific contexts
using MassTransit.KafkaIntegration;  // For Kafka specific contexts
using MassTransit.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Authentication; // For SslProtocols
using Confluent.Kafka; // For Kafka configs like SecurityProtocol, SaslMechanism
using TemporaryName.Application.Contracts.Abstractions.Messaging;
using TemporaryName.Infrastructure.Messaging.MassTransit.Filters; // Assuming you create custom filters
using TemporaryName.Infrastructure.Messaging.MassTransit.Middlewares; // Assuming you create custom middlewares
using TemporaryName.Infrastructure.Messaging.MassTransit.Publishers;
using TemporaryName.Infrastructure.Messaging.MassTransit.Settings;
// using MyApplicationDbContext = TemporaryName.Infrastructure.Persistence.AppDbContext; // Placeholder for your EF DbContext

namespace TemporaryName.Infrastructure.Messaging.MassTransit;

public static class DependencyInjection
{
    public static IServiceCollection AddMassTransitMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly[]? assembliesToScanForConsumers = null,
        Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null)
    {
        services.Configure<MassTransitOptions>(configuration.GetSection(MassTransitOptions.SectionName));
        // Bind individual transport options for easier access if needed, though MassTransitOptions is the main one.
        services.Configure<RabbitMqOptions>(configuration.GetSection($"{MassTransitOptions.SectionName}:{nameof(MassTransitOptions.RabbitMq)}"));
        services.Configure<KafkaOptions>(configuration.GetSection($"{MassTransitOptions.SectionName}:{nameof(MassTransitOptions.Kafka)}"));

        services.AddSingleton<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();

        // For EF Core Outbox if used by MassTransit directly
        // string? efDbContextTypeName = configuration[$"{MassTransitOptions.SectionName}:RabbitMq:EntityFrameworkCoreOutboxDbContextTypeFullName"] ??
        //                             configuration[$"{MassTransitOptions.SectionName}:Kafka:EntityFrameworkCoreOutboxDbContextTypeFullName"];
        // Type? dbContextType = !string.IsNullOrEmpty(efDbContextTypeName) ? Type.GetType(efDbContextTypeName) : null;
        // if (dbContextType == null && (!string.IsNullOrEmpty(efDbContextTypeName)))
        // {
        //     // Log warning: DbContext for MassTransit outbox not found
        // }


        services.AddMassTransit(mtConfig =>
        {
            MassTransitOptions mtGlobalOptions = configuration.GetSection(MassTransitOptions.SectionName).Get<MassTransitOptions>()!;
            if (mtGlobalOptions == null) throw new InvalidOperationException("MassTransitOptions section is missing.");

            // --- Global Serializer: Protobuf ---
            mtConfig.AddProtobufSerializer(options =>
            {
                // options.Identifier = MessagePack.MessagePackSerializer. ด้วย MessagePack; // Example if using MessagePack for some parts
            });
            // mtConfig.SetMessageSerializer(ProtobufMessageSerializer.ProtobufContentType); // Set as default for all messages

            // --- Global Entity Name Formatter ---
            mtConfig.SetEntityNameFormatter(GetEntityNameFormatter(mtGlobalOptions.EntityNameFormatter, mtGlobalOptions.ServiceName));


            // --- Consumer/Saga Registration ---
            if (assembliesToScanForConsumers != null && assembliesToScanForConsumers.Length > 0)
            {
                mtConfig.AddConsumers(assembliesToScanForConsumers);
                mtConfig.AddSagas(assembliesToScanForConsumers);
                // Example: mtConfig.AddSagaStateMachine<MyOrderSaga, MyOrderSagaState>().InMemoryRepository();
                // Or .EntityFrameworkRepository(r => { /* EF Core Saga State Repo Config */ });
            }
            busRegistrationConfigurator?.Invoke(mtConfig);


            // --- Transport Selection & Configuration ---
            switch (mtGlobalOptions.MessageBrokerType?.ToUpperInvariant())
            {
                case "RABBITMQ":
                    ConfigureRabbitMqTransport(mtConfig, mtGlobalOptions.RabbitMq, mtGlobalOptions);
                    break;
                case "KAFKA":
                    ConfigureKafkaTransport(mtConfig, mtGlobalOptions.Kafka, mtGlobalOptions);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported MessageBrokerType: {mtGlobalOptions.MessageBrokerType}.");
            }

            // --- Request Client Configuration (Example) ---
            // mtConfig.AddRequestClient<MyRequestMessage>(TimeSpan.FromSeconds(mtGlobalOptions.DefaultTimeoutMs / 1000.0));
        });

        // --- OpenTelemetry ---
        if (mtGlobalOptions.EnableOpenTelemetry)
        {
            services.AddOpenTelemetry().WithTracing(builder =>
            {
                builder.AddSource(DiagnosticHeaders.DefaultListenerName) // MassTransit main diagnostic source
                       .AddSource("MassTransit.Transport.RabbitMQ") // Specific transport traces
                       .AddSource("MassTransit.Transport.Kafka");
                // Add other sources: AspNetCore, HttpClient, EFCore, GrpcClient, etc.
                // Example: .AddAspNetCoreInstrumentation()
                //         .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(mtGlobalOptions.ServiceName ?? "UnnamedMassTransitService"));
            });
        }

        // --- MassTransit Health Checks ---
        services.AddHealthChecks().AddMassTransitCheck("masstransit_bus_status", mtGlobalOptions);


        // --- Optional: Rider for producing to Kafka topics not directly managed by MassTransit consumers ---
        // This allows type-safe production to Kafka topics using MassTransit's infrastructure.
        // if (mtGlobalOptions.MessageBrokerType?.ToUpperInvariant() == "KAFKA" && mtGlobalOptions.Kafka.Topics?.Any() == true)
        // {
        //     services.AddMassTransitRider(riderConfig =>
        //     {
        //         KafkaOptions kafkaOptions = mtGlobalOptions.Kafka;
        //         foreach (var topicEntry in kafkaOptions.Topics.Where(t => t.Value.IsProducerTopic)) // Add a flag IsProducerTopic to KafkaTopicOptions
        //         {
        //             // Assumes message type can be resolved or is a generic type like byte[] or JsonDocument
        //             // Type messageType = ResolveMessageTypeForTopic(topicEntry.Key);
        //             // riderConfig.AddProducer(topicEntry.Value.Name, messageType);
        //         }
        //
        //         riderConfig.UsingKafka((context, k) =>
        //         {
        //             k.Host(kafkaOptions.BootstrapServers);
        //             ConfigureKafkaClientSecurity(k, kafkaOptions.Security);
        //         });
        //     });
        // }

        return services;
    }

    private static IEntityNameFormatter GetEntityNameFormatter(string formatterName, string? serviceName)
    {
        string prefix = string.IsNullOrWhiteSpace(serviceName) ? "" : $"{serviceName}-";
        return formatterName?.ToUpperInvariant() switch
        {
            "KEBABCASE" => new KebabCaseEntityNameFormatter(prefix),
            "SNAKECASE" => new SnakeCaseEntityNameFormatter(prefix),
            "PASCALCASE" => new PascalCaseEntityNameFormatter(prefix),
            _ => new MessageUrnEntityNameFormatter(), // MassTransit's default, based on message type
        };
    }


    private static void ConfigureRabbitMqTransport(IBusRegistrationConfigurator mtConfig, RabbitMqOptions options, MassTransitOptions globalOptions)
    {
        if (options == null) throw new ArgumentNullException(nameof(options), "RabbitMqOptions are not configured.");

        mtConfig.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(options.Host, options.Port, options.VirtualHost, hostCfg =>
            {
                hostCfg.Username(options.Username);
                hostCfg.Password(options.Password);
                hostCfg.RequestedConnectionTimeout(TimeSpan.FromMilliseconds(options.RequestedConnectionTimeoutMs));
                hostCfg.Heartbeat(TimeSpan.FromSeconds(options.RequestedHeartbeatSecs));
                if (options.UseNagleAlgorithm) hostCfg.UseNagleAlgorithm();

                if (options.Ssl.Enabled)
                {
                    hostCfg.UseSsl(ssl =>
                    {
                        ssl.ServerName = options.Ssl.ServerName;
                        if (!string.IsNullOrEmpty(options.Ssl.CertPath)) ssl.CertificatePath = options.Ssl.CertPath;
                        if (!string.IsNullOrEmpty(options.Ssl.CertPassphrase)) ssl.CertificatePassphrase = options.Ssl.CertPassphrase;
                        if (options.Ssl.Protocol.HasValue) ssl.Protocol = options.Ssl.Protocol.Value;
                        ssl.CheckCertificateRevocation = options.Ssl.CheckCertificateRevocation;
                        if (!string.IsNullOrEmpty(options.Ssl.AcceptablePolicyErrors))
                        {
                            foreach (var errorStr in options.Ssl.AcceptablePolicyErrors.Split(','))
                            {
                                if (Enum.TryParse<System.Net.Security.SslPolicyErrors>(errorStr.Trim(), true, out var policyError))
                                {
                                    ssl.AcceptablePolicyErrors |= policyError;
                                }
                            }
                        }
                    });
                }
                if (options.ClientProvidedConnectionProperties != null)
                {
                    foreach(var prop in options.ClientProvidedConnectionProperties)
                    {
                        hostCfg.ClientProvidedName += $"; {prop.Key}:{prop.Value}"; // Simplified, RabbitMQ .NET client has a specific way
                    }
                }
            });

            cfg.PrefetchCount = (int)(globalOptions.GlobalPrefetchCount ?? options.PrefetchCount);
            if (options.ConcurrencyLimit.HasValue) cfg.ConcurrentMessageLimit = options.ConcurrencyLimit;

            if (options.UseDelayedExchangeMessageScheduler) cfg.UseDelayedExchangeMessageScheduler();

            if (options.PublisherConfirmations.Enabled)
            {
                cfg.UsePublisherConfirmation(TimeSpan.FromMilliseconds(options.PublisherConfirmations.TimeoutMs));
            }

            // --- Global Filters/Middleware for RabbitMQ ---
            // cfg.UseSendFilter(typeof(ExampleSendAuditFilter<>), context); // Example custom send filter
            // cfg.UseConsumeFilter(typeof(ExampleConsumeValidationFilter<>), context); // Example custom consume filter
            // cfg.UseRateLimiter(new RateLimitOptions(100, TimeSpan.FromSeconds(1))); // Example global rate limiter
            // cfg.UseCircuitBreaker(cb => { cb.TripThreshold = 15; cb.ActiveThreshold = 10; cb.ResetInterval = TimeSpan.FromMinutes(5); });

            // --- Retry & Error Handling ---
            ConfigureConsumerRetry(cfg, options.ConsumerRetry);
            ConfigureRabbitMqDeadLetter(cfg, options.DeadLetterStrategy.RabbitMq, context);

            // --- MassTransit Outbox ---
            if (options.UseEntityFrameworkCoreOutbox && !string.IsNullOrEmpty(options.EntityFrameworkCoreOutboxDbContextTypeFullName))
            {
                Type? dbContextType = Type.GetType(options.EntityFrameworkCoreOutboxDbContextTypeFullName);
                if (dbContextType != null)
                {
                    // Dynamically call cfg.UseEntityFrameworkOutbox<TDbContext>(context);
                    var method = typeof(EntityFrameworkOutboxConfigurationExtensions)
                        .GetMethod(nameof(EntityFrameworkOutboxConfigurationExtensions.UseEntityFrameworkOutbox))
                        ?.MakeGenericMethod(dbContextType);
                    method?.Invoke(null, new object[] { cfg, context, (Action<IOutboxOptions>) (outboxOption => {
                        outboxOption.QueryDelay = TimeSpan.FromMilliseconds(options.EntityFrameworkCoreOutboxQueryDelayMs);
                        outboxOption.QueryMessageLimit = options.EntityFrameworkCoreOutboxQueryMessageLimit;
                        outboxOption.DuplicateDetectionWindow = TimeSpan.FromMinutes(30); // Default
                    })});
                     _ = LogContext.Info?.Log("Configured Entity Framework Core Outbox with {DbContext}", dbContextType.Name);
                }
                else {  _ = LogContext.Warning?.Log("Could not find DbContext type {DbContextName} for MassTransit EF Core Outbox.", options.EntityFrameworkCoreOutboxDbContextTypeFullName); }
            }
            else if (options.UseInMemoryOutbox)
            {
                cfg.UseInMemoryOutbox(context);
            }

            if (options.UseSingleActiveConsumer) cfg.SingleActiveConsumer = true;

            cfg.ConfigureEndpoints(context, new RabbitMqEndpointFactory()); // Pass custom factory if needed

            _ = LogContext.ConfigureCurrentLogContext(context.GetService<ILoggerFactory>());
        });
    }


    private static void ConfigureKafkaTransport(IBusRegistrationConfigurator mtConfig, KafkaOptions options, MassTransitOptions globalOptions)
    {
        if (options == null) throw new ArgumentNullException(nameof(options), "KafkaOptions are not configured.");

        mtConfig.UsingKafka((context, cfg) =>
        {
            var hostName = options.ClientIdPrefix ?? globalOptions.ServiceName ?? Assembly.GetEntryAssembly()?.GetName().Name ?? "masstransit-kafka-client";
            cfg.Host(options.BootstrapServers, hostCfg =>
            {
                ConfigureKafkaClientSecurity(hostCfg, options.Security);
            });

            // --- Global Kafka Producer Settings ---
            cfg.UseKafkaSendPipelineConfiguration((sendCfg, sendContext) => {
                sendCfg.EnableIdempotence = options.DefaultProducer.EnableIdempotence;
                if (options.DefaultProducer.Acks.HasValue) sendCfg.Acks = options.DefaultProducer.Acks;
                // Set other producer defaults from options.DefaultProducer
            });


            // --- Topic and Consumer Group Configuration from Options ---
            // This requires consumers to be defined with endpoint configurators that specify their group and topic.
            // MassTransit's ConfigureEndpoints will set up consumers. Here we can pre-configure topics if needed.
            if (options.Topics != null)
            {
                foreach (var topicEntry in options.Topics)
                {
                    KafkaTopicOptions topicOpts = topicEntry.Value;
                    if (topicOpts.AutoCreate)
                    {
                        cfg.CreateTopicEndpoint(topicOpts.Name, topicOpts.NumPartitions, (short)topicOpts.ReplicationFactor, topicConfig =>
                        {
                            if (topicOpts.Configs != null)
                            {
                                foreach (var kvp in topicOpts.Configs) topicConfig.Set(kvp.Key, kvp.Value);
                            }
                        });
                         _ = LogContext.Info?.Log("Configured Kafka topic endpoint for {TopicName} with auto-create.", topicOpts.Name);
                    }
                }
            }
            // Consumer group configurations from options.ConsumerGroups would be applied when defining ReceiveEndpoints/TopicEndpoints.
            // For example:
            // cfg.TopicEndpoint<MyMessage>("topic-name", "consumer-group-from-options", e => {
            //    var groupOpts = options.ConsumerGroups["my-group-key"];
            //    e.ConcurrentMessageLimit = groupOpts.ConcurrencyLimit;
            //    e.AutoOffsetReset = groupOpts.AutoOffsetReset;
            //    ConfigureConsumerRetry(e, options.ConsumerRetry);
            //    ConfigureKafkaDeadLetter(e, options.DeadLetterStrategy.Kafka, context);
            // });


            // --- Retry & Error Handling (global for Kafka consumers if not set per endpoint) ---
            ConfigureConsumerRetry(cfg, options.ConsumerRetry); // Global retry for Kafka consumers
            // Global DLQ for Kafka (less common, usually per-endpoint)
            // ConfigureKafkaDeadLetter(cfg, options.DeadLetterStrategy.Kafka, context);


            // --- MassTransit Outbox ---
            if (options.UseEntityFrameworkCoreOutbox && !string.IsNullOrEmpty(options.EntityFrameworkCoreOutboxDbContextTypeFullName))
            {
                 Type? dbContextType = Type.GetType(options.EntityFrameworkCoreOutboxDbContextTypeFullName);
                 if (dbContextType != null)
                 {
                     var method = typeof(EntityFrameworkOutboxConfigurationExtensions)
                         .GetMethod(nameof(EntityFrameworkOutboxConfigurationExtensions.UseEntityFrameworkOutbox))
                         ?.MakeGenericMethod(dbContextType);
                     method?.Invoke(null, new object[] { cfg, context, (Action<IOutboxOptions>) (outboxOption => {
                         outboxOption.QueryDelay = TimeSpan.FromMilliseconds(options.EntityFrameworkCoreOutboxQueryDelayMs);
                         outboxOption.QueryMessageLimit = options.EntityFrameworkCoreOutboxQueryMessageLimit;
                     })});
                 } else { /* Log warning */ }
            }
            else if (options.UseInMemoryOutbox)
            {
                cfg.UseInMemoryOutbox(context);
            }


            cfg.ConfigureEndpoints(context, new KafkaEndpointFactory()); // Pass custom factory if needed

            _ = LogContext.ConfigureCurrentLogContext(context.GetService<ILoggerFactory>());
        });
    }


    private static void ConfigureKafkaClientSecurity(IKafkaHostConfigurator hostCfg, KafkaSecurityOptions secOpts)
    {
        if (secOpts.SecurityProtocol.HasValue) hostCfg.SecurityProtocol = secOpts.SecurityProtocol;
        if (secOpts.SaslMechanism.HasValue)
        {
            hostCfg.SaslMechanism = secOpts.SaslMechanism;
            hostCfg.SaslUsername = secOpts.SaslUsername;
            hostCfg.SaslPassword = secOpts.SaslPassword;
            if (!string.IsNullOrEmpty(secOpts.SaslOauthbearerConfig)) hostCfg.SaslOauthbearerConfig = secOpts.SaslOauthbearerConfig;
        }
        if (!string.IsNullOrEmpty(secOpts.SslCaLocation)) hostCfg.SslCaLocation = secOpts.SslCaLocation;
        if (!string.IsNullOrEmpty(secOpts.SslCertificateLocation)) hostCfg.SslCertificateLocation = secOpts.SslCertificateLocation;
        if (!string.IsNullOrEmpty(secOpts.SslKeyLocation)) hostCfg.SslKeyLocation = secOpts.SslKeyLocation;
        if (!string.IsNullOrEmpty(secOpts.SslKeyPassword)) hostCfg.SslKeyPassword = secOpts.SslKeyPassword;
        if (secOpts.EnableSslCertificateVerification.HasValue) hostCfg.EnableSslCertificateVerification = secOpts.EnableSslCertificateVerification;
        if (secOpts.SslEndpointIdentificationAlgorithm.HasValue) hostCfg.SslEndpointIdentificationAlgorithm = secOpts.SslEndpointIdentificationAlgorithm;
    }

    private static void ConfigureConsumerRetry<TEndpointConfigurator>(
        TEndpointConfigurator configurator, // IReceiveEndpointConfigurator, IKafkaTopicEndpointConfigurator, etc.
        ConsumerRetryOptions retryOptions) where TEndpointConfigurator : IConsumePipeConfigurator
    {
        if (retryOptions.Strategy == RetryStrategy.None) return;

        var retryPolicy = GreenPipes.Policies.RetryConfigurationExtensions.CreatePolicy(r => // Use GreenPipes.Policies directly
        {
            // Filter exceptions
            if (retryOptions.HandleExceptionTypes.Any())
            {
                foreach (string exTypeName in retryOptions.HandleExceptionTypes)
                {
                    Type? exType = Type.GetType(exTypeName);
                    if (exType != null && typeof(Exception).IsAssignableFrom(exType))
                    {
                         r.GetType()
                          .GetMethod("Handle", new[] { typeof(Type[]) })?
                          .MakeGenericMethod(exType)
                          .Invoke(r, new object[] { Array.Empty<Type>() });
                        // r.Handle(exType); // Simpler if direct method exists or via reflection
                    } else { /* Log warning: unknown exception type to handle */ }
                }
            } else { r.Handle<Exception>(); } // Default to all if not specified

            if (retryOptions.IgnoreExceptionTypes.Any())
            {
                 foreach (string exTypeName in retryOptions.IgnoreExceptionTypes)
                 {
                     Type? exType = Type.GetType(exTypeName);
                     if (exType != null && typeof(Exception).IsAssignableFrom(exType))
                     {
                         r.GetType()
                          .GetMethod("Ignore", new[] { typeof(Type[]) })?
                          .MakeGenericMethod(exType)
                          .Invoke(r, new object[] { Array.Empty<Type>() });
                         // r.Ignore(exType);
                     } else { /* Log warning: unknown exception type to ignore */ }
                 }
            }

            switch (retryOptions.Strategy)
            {
                case RetryStrategy.Immediate:
                    r.Immediate(retryOptions.RetryLimit);
                    break;
                case RetryStrategy.Interval:
                    r.Intervals(retryOptions.RetryLimit, retryOptions.IntervalScheduleMs.Select(ms => TimeSpan.FromMilliseconds(ms)).ToArray());
                    break;
                case RetryStrategy.Incremental:
                    r.Incremental(retryOptions.RetryLimit, TimeSpan.FromMilliseconds(retryOptions.IncrementalInitialIntervalMs), TimeSpan.FromMilliseconds(retryOptions.IncrementalIntervalIncrementMs));
                    break;
                case RetryStrategy.Exponential:
                    r.Exponential(retryOptions.RetryLimit, TimeSpan.FromMilliseconds(retryOptions.ExponentialMinIntervalMs), TimeSpan.FromMilliseconds(retryOptions.ExponentialMaxIntervalMs), TimeSpan.FromMilliseconds(retryOptions.ExponentialFactor)); // Factor is used as interval delta in some overloads. Check MassTransit's exact behavior for exponential.
                    break;
            }
        });
        configurator.UseRetry(retryPolicy);
    }

    private static void ConfigureRabbitMqDeadLetter(IRabbitMqReceiveEndpointConfigurator cfg, RabbitMqDeadLetterOptions dlqOptions, IBusRegistrationContext context)
    {
        if (!dlqOptions.Enabled)
        {
            // If DLQ is disabled, failed messages after retries might be discarded or requeued based on broker defaults.
            // This is generally not recommended for important messages.
            cfg.DiscardFaultedMessages(); // Explicitly discard if that's the desired behavior
            return;
        }

        if (!string.IsNullOrEmpty(dlqOptions.CentralizedDeadLetterExchangeName))
        {
            cfg.DeadLetterExchange = dlqOptions.CentralizedDeadLetterExchangeName;
            // Optionally set a routing key for the dead letter exchange if needed
            // You might need to configure the dead letter exchange separately (e.g., as a fanout or topic)
            // And bind a queue to it for collecting all dead letters.
        }
        // else, MassTransit uses convention: an _error queue bound to the main queue's DLX.

        if (dlqOptions.ErrorQueueTtlMs.HasValue)
        {
            cfg.SetQueueArgument("x-message-ttl", dlqOptions.ErrorQueueTtlMs.Value);
        }

        // For more advanced DLQ, like publishing to a different exchange with enriched info:
        // cfg.Use μετα(context => new DeadLetterPublishFilter(context.GetRequiredService<IPublishEndpoint>(), dlqOptions));
    }

    private static void ConfigureKafkaDeadLetter(IKafkaTopicEndpointConfigurator cfg, KafkaDeadLetterTopicOptions dlqOptions, IBusRegistrationContext context)
    {
        if (!dlqOptions.Enabled)
        {
            // Kafka doesn't have broker-side DLQs. If disabled, messages are either committed (lost if error) or not (reprocessed).
            // Consider a KillSwitch for persistently failing messages if no DLT.
            // cfg.UseKillSwitch(options => options
            //    .SetActivationThreshold(10)
            //    .SetTripThreshold(0.15) // 15% failure rate
            //    .SetRestartTimeout(m: 1)); // Restart after 1 minute
            return;
        }

        string deadLetterTopic = dlqOptions.DeadLetterTopicNameFormat.Replace("{OriginalTopic}", cfg.TopicName);

        // This is a conceptual way to pipe to DLT. MassTransit might offer more integrated ways.
        // One common pattern is to use a filter or middleware on the retry policy's final attempt.
        cfg.UseScheduledRedelivery(r => r.Intervals(TimeSpan.FromSeconds(10))); // Ensure some redelivery before DLT
        cfg.UseMessageRetry(r => {
            r.Immediate(5); // Example: retry a few times quickly
            r.Finally(f =>
            {
                f.UseFilter(new KafkaDeadLetterTransportFilter(deadLetterTopic, context.GetService<ILogger<KafkaDeadLetterTransportFilter>>(), dlqOptions.AddDiagnosticHeaders));
            });
        });


        // Ensure the DLT exists (if auto-create is enabled)
        // This is complex to do directly in consumer config. Usually done by an admin client or separate process.
        // MassTransit's CreateTopicEndpoint can be used if this DLT is also consumed for monitoring.
        if (dlqOptions.CreateDeadLetterTopic)
        {
            // This is tricky here. Topic creation is usually on the bus configurator (cfg.Host).
            // We might need a separate mechanism or rely on Kafka auto-creation if enabled on broker.
            // For now, assume it's created or auto-creation is enabled.
             _ = LogContext.Info?.Log("Kafka DLT configured for topic {OriginalTopic} -> {DeadLetterTopic}. Ensure DLT exists or is auto-created.", cfg.TopicName, deadLetterTopic);
        }
    }
}

// Custom filter for Kafka DLT (simplified example)
// Place in a new file, e.g., Filters/KafkaDeadLetterTransportFilter.cs
public class KafkaDeadLetterTransportFilter : IFilter<ConsumeContext>
{
    private readonly string _deadLetterTopic;
    private readonly ILogger<KafkaDeadLetterTransportFilter> _logger;
    private readonly bool _addDiagnosticHeaders;

    public KafkaDeadLetterTransportFilter(string deadLetterTopic, ILogger<KafkaDeadLetterTransportFilter> logger, bool addDiagnosticHeaders)
    {
        _deadLetterTopic = deadLetterTopic;
        _logger = logger;
        _addDiagnosticHeaders = addDiagnosticHeaders;
    }

    public async Task Send(ConsumeContext context, IPipe<ConsumeContext> next)
    {
        try
        {
            await next.Send(context);
        }
        catch (Exception ex)
        {
            if (context.GetRetryAttempt() >= (context.GetRetryLimit() ?? 0) || context.IsFaulted ) // Check if it's the final attempt or already faulted
            {
                _logger.LogError(ex, "Message {MessageId} failed after all retries or is faulted. Forwarding to DLT: {DeadLetterTopic}", context.MessageId, _deadLetterTopic);
                try
                {
                    // Need a producer to send to the DLT. This should be injected or resolved.
                    // For simplicity, this shows conceptual forwarding.
                    // In a real scenario, use ITopicProducer from MassTransit Rider or a dedicated Kafka producer.
                    var producerFactory = context.GetPayload<IBusInstance>().Host잖아요별도의_카프카_프로듀서를_만들어야_합니다; //.Topology.SendTopology.GetMessageTopology(context.Message.GetType()).GetSendAddress()

                    // This is highly conceptual and needs a proper Kafka producer instance.
                    // Using IPublishEndpoint here is generally for publishing through MassTransit's configured bus,
                    // not raw Kafka production to an arbitrary topic unless that topic is also configured in MassTransit.
                    // For true DLT forwarding, you'd often use a KafkaProducer directly or MassTransit.Rider.
                    var forwarder = context.GetRequiredService<ITopicProducerProvider>(); // if using Rider
                    var producer = await forwarder.GetProducer<object>(new Uri($"topic:{_deadLetterTopic}"));


                    var headersToForward = new Dictionary<string, object>();
                    if (_addDiagnosticHeaders)
                    {
                        headersToForward["MT-Dlx-Exception-Message"] = ex.Message;
                        headersToForward["MT-Dlx-Exception-StackTrace"] = ex.StackTrace ?? string.Empty;
                        headersToForward["MT-Dlx-Exception-Type"] = ex.GetType().FullName ?? string.Empty;
                        headersToForward["MT-Dlx-Original-Topic"] = context.ReceiveContext.InputAddress?.GetEndpointName() ?? "unknown-topic"; // May need adjustment
                        headersToForward["MT-Dlx-Timestamp"] = DateTime.UtcNow.ToString("o");
                    }
                    foreach (var header in context.Headers.GetAll()) { headersToForward.TryAdd(header.Key, header.Value); }


                    // Re-wrap the original message bytes if possible, or the deserialized message.
                    // This depends on what you want in your DLT message.
                    object messageToDlt = context.Message ?? context.SupportedMessageTypes.FirstOrDefault() switch {
                        Type T when T != null => context.ReceiveContext.TransportHeaders.Get("MT-Fault-Message", default(Newtonsoft.Json.Linq.JToken))?.ToObject(T) , // if fault message available
                        _ => null
                    };

                    if (messageToDlt != null) {
                        await producer.Produce(messageToDlt, context.Create PřiřazeníPipe(p => p.Headers.Set(headersToForward)));
                    } else {
                         _logger.LogWarning("Could not retrieve original message for DLT forwarding for MessageId {MessageId}", context.MessageId);
                    }

                    // After successfully sending to DLT, the message should be ACKNOWLEDGED on the original topic.
                    // MassTransit handles this by not throwing the exception further up from this filter
                    // if the DLT publish was successful.
                    return; // Do not re-throw if DLT publish is successful
                }
                catch (Exception dltEx)
                {
                    _logger.LogCritical(dltEx, "Failed to forward message {MessageId} to DLT {DeadLetterTopic}. Original exception: {OriginalException}", context.MessageId, _deadLetterTopic, ex.Message);
                }
            }
            throw; // Re-throw original exception if not final attempt or DLT failed
        }
    }
    public void Probe(ProbeContext context) { context.CreateFilterScope("kafkaDeadLetterTransport"); }
}

// Helper for MassTransit Health Check registration
public static class MassTransitHealthCheckExtensions
{
    public static IHealthChecksBuilder AddMassTransitCheck(this IHealthChecksBuilder builder, string name, MassTransitOptions options, HealthStatus? failureStatus = null, IEnumerable<string>? tags = null, TimeSpan? timeout = null)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            sp => {
                var busControl = sp.GetService<IBusControl>(); // For single bus
                // If multiple named buses: var busDepot = sp.GetService<IBusDepot>();
                // var busControl = busDepot.GetBus(options.RabbitMq.BusInstanceName ?? options.Kafka.BusInstanceName ?? "default");
                if (busControl == null) return new HealthCheckResultProvider(HealthStatus.Unhealthy, "MassTransit bus not found in service provider.");
                return new MassTransitHealthCheck(busControl, options);
            },
            failureStatus,
            tags,
            timeout
        ));
    }
}

// Actual Health Check Implementation
public class MassTransitHealthCheck : IHealthCheck
{
    private readonly IBusControl _busControl;
    private readonly MassTransitOptions _options;

    public MassTransitHealthCheck(IBusControl busControl, MassTransitOptions options)
    {
        _busControl = busControl;
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // More in-depth checks can be performed here, e.g., probing endpoints
            // For now, a simple check if the bus is started.
            // var ready = await _busControl.WaitForHealthStatus(BusHealthStatus.Healthy, TimeSpan.FromSeconds(5), cancellationToken);
            // A more direct check might involve querying endpoint health if available.

            // A very basic check:
            if (_busControl.Address == null) // Not fully initialized or stopped
            {
                 return HealthCheckResult.Unhealthy($"MassTransit bus '{_options.ServiceName}' is not available or address is null.");
            }

            // For a deeper check, you might iterate through receive endpoint health.
            // This is not straightforward with IBusControl directly.
            // Health checks often rely on the transport-specific AddMassTransitHealthCheck methods.

            // This is a simplified check. The built-in `AddMassTransit()` health check is more thorough.
            // This custom one is mostly for demonstration if you need finer control.
            // The `services.AddHealthChecks().AddMassTransit()` is generally preferred.
            return HealthCheckResult.Healthy($"MassTransit bus '{_options.ServiceName}' is responsive.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"MassTransit bus '{_options.ServiceName}' health check failed: {ex.Message}");
        }
    }
}

// Dummy endpoint factories for ConfigureEndpoints (replace with actual if you customize factories)
public class RabbitMqEndpointFactory : IRabbitMqEndpointFactory { /* ... */ }
public class KafkaEndpointFactory : IKafkaEndpointFactory { /* ... */ }