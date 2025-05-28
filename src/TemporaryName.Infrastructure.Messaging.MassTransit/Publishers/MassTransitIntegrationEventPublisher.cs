using MassTransit;
using Microsoft.Extensions.DependencyInjection; // For IServiceProvider
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using TemporaryName.Application.Contracts.Abstractions.Messaging;
using TemporaryName.Domain.Primitives.DomainEvent; // Your IDomainEvent
using TemporaryName.Infrastructure.Messaging.MassTransit.Contracts; // For IEventMapper
using TemporaryName.Infrastructure.Messaging.MassTransit.Exceptions;
using TemporaryName.Domain.Primitives.Contextual; // For MessagingConfigurationException

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Publishers;

public partial class MassTransitIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MassTransitIntegrationEventPublisher> _logger;
    private readonly IServiceProvider _serviceProvider;

    // Cache for mapping POCO FQN string to its .NET Type
    private static readonly ConcurrentDictionary<string, Type> _pocoDomainEventTypeMap = new();
    // Cache for mapping POCO Type to a delegate that resolves and uses the specific IEventMapper<TPoco, TProto>
    private static readonly ConcurrentDictionary<Type, Func<IDomainEvent, IServiceProvider, IMessage>> _pocoToProtoMapperDelegates = new();

    private static bool _typesScannedAndMapped = false;
    private static readonly object _scanLock = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public MassTransitIntegrationEventPublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<MassTransitIntegrationEventPublisher> logger,
        IServiceProvider serviceProvider,
        params Assembly[]? assembliesToScanForMappers) // Optionally pass assemblies, or rely on AppDomain.CurrentDomain
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = false };

        // Ensure assembliesToScanForMappers includes assemblies where IDomainEvent and IEventMapper implementations reside.
        // If null or empty, it will scan AppDomain.CurrentDomain assemblies matching certain prefixes.
        ScanAndMapEventTypesOnce(serviceProvider, assembliesToScanForMappers);
    }

    private void ScanAndMapEventTypesOnce(IServiceProvider serviceProvider, Assembly[]? explicitAssembliesToScan)
    {
        if (_typesScannedAndMapped) return;
        lock (_scanLock)
        {
            if (_typesScannedAndMapped) return;

            LogMappingScanStarted(_logger);

            var assembliesToScan = explicitAssembliesToScan?.Any() == true
                ? explicitAssembliesToScan.ToList()
                : AppDomain.CurrentDomain.GetAssemblies()
                    .Where(asm => !asm.IsDynamic &&
                                  (asm.FullName?.StartsWith("TemporaryName.Domain", StringComparison.OrdinalIgnoreCase) == true ||
                                   asm.FullName?.StartsWith("TemporaryName.Application", StringComparison.OrdinalIgnoreCase) == true ||
                                   asm.FullName?.StartsWith("TemporaryName.Infrastructure", StringComparison.OrdinalIgnoreCase) == true || // For mappers in Infrastructure
                                   asm.FullName?.StartsWith(Assembly.GetEntryAssembly()?.GetName().Name ?? "ENTRY_ASSEMBLY_FALLBACK", StringComparison.OrdinalIgnoreCase) == true))
                    .ToList();

            if (!assembliesToScan.Any())
            {
                LogNoAssembliesFoundForScan(_logger, explicitAssembliesToScan?.Any() == true ? "explicitly provided" : "AppDomain heuristics");
                _typesScannedAndMapped = true; // Mark as scanned to prevent re-scans, even if nothing found.
                return;
            }

            LogFoundAssembliesForScan(_logger, string.Join(", ", assembliesToScan.Select(a => a.GetName().Name)));

            var allRelevantTypesFromAssemblies = assembliesToScan
                .SelectMany(SafeGetTypes)
                .Where(t => t != null) // Ensure t is not null before further processing
                .ToList();


            // 1. Discover POCO IDomainEvent implementations
            var pocoDomainEventTypes = allRelevantTypesFromAssemblies
                .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && !typeof(IMessage).IsAssignableFrom(t))
                .ToList();

            foreach (Type pocoType in pocoDomainEventTypes)
            {
                if (pocoType.FullName != null)
                {
                    _pocoDomainEventTypeMap.TryAdd(pocoType.FullName, pocoType);
                    LogPocoEventTypeDiscovered(_logger, pocoType.FullName);
                }
                else
                {
                    LogPocoEventTypeDiscoveredWithNullFullName(_logger, pocoType.Name);
                }
            }

            // 2. Discover and prepare IEventMapper<TDomain, TProto> implementations
            var mapperImplementationTypes = allRelevantTypesFromAssemblies
                .Where(t => !t.IsAbstract && !t.IsInterface && t.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventMapper<,>)))
                .ToList();

            foreach (Type mapperImplType in mapperImplementationTypes)
            {
                var mapperInterfaces = mapperImplType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventMapper<,>));

                foreach (var mapperInterface in mapperInterfaces)
                {
                    Type domainEventType = mapperInterface.GetGenericArguments()[0];
                    Type integrationEventType = mapperInterface.GetGenericArguments()[1]; // This is TIntegrationEvent : IMessage

                    if (!typeof(IDomainEvent).IsAssignableFrom(domainEventType))
                    {
                        LogInvalidMapperDomainType(_logger, mapperInterface.FullName ?? mapperImplType.Name, domainEventType.FullName ?? "Unknown");
                        continue;
                    }
                    if (!typeof(IMessage).IsAssignableFrom(integrationEventType))
                    {
                        LogInvalidMapperIntegrationType(_logger, mapperInterface.FullName ?? mapperImplType.Name, integrationEventType.FullName ?? "Unknown");
                        continue;
                    }

                    // Create a delegate that resolves the mapper from DI and calls its Map method
                    _pocoToProtoMapperDelegates.TryAdd(domainEventType, (domainEvent, sp) =>
                    {
                        var resolvedMapper = sp.GetService(mapperInterface);
                        if (resolvedMapper == null)
                        {
                            LogEventMapperNotResolvedFromDI(_logger, mapperInterface.FullName ?? "UnknownMapper", domainEventType.FullName ?? "UnknownPoco");
                            throw new MessagingConfigurationException(
                                $"Event mapper '{mapperInterface.FullName}' for domain event '{domainEventType.FullName}' could not be resolved from DI. Ensure it is registered.",
                                "MAPPER_NOT_REGISTERED");
                        }

                        var mapMethod = mapperInterface.GetMethod("Map", new[] { domainEventType });
                        if (mapMethod == null)
                        {
                            LogEventMapMethodNotFoundOnMapper(_logger, mapperInterface.FullName ?? "UnknownMapper", domainEventType.FullName ?? "UnknownPoco");
                            throw new MissingMethodException($"CRITICAL: 'Map' method not found on mapper '{mapperInterface.FullName}' for domain event '{domainEventType.FullName}'. This indicates a problem with the IEventMapper interface or implementation.");
                        }

                        try
                        {
                            var mappedProtoEvent = mapMethod.Invoke(resolvedMapper, new[] { domainEvent }) as IMessage;
                            if (mappedProtoEvent == null)
                            {
                                LogEventMappingExecutionReturnedNull(_logger, mapperInterface.FullName ?? "UnknownMapper", domainEventType.FullName ?? "UnknownPoco");
                                throw new InvalidOperationException($"Execution of 'Map' method on mapper '{mapperInterface.FullName}' for domain event '{domainEventType.FullName}' returned null. Mapped Protobuf event cannot be null.");
                            }
                            return mappedProtoEvent;
                        }
                        catch(TargetInvocationException tie) when (tie.InnerException != null)
                        {
                            LogEventMappingExecutionThrewException(_logger, mapperInterface.FullName ?? "UnknownMapper", domainEventType.FullName ?? "UnknownPoco", tie.InnerException);
                            throw tie.InnerException; // Re-throw the actual exception from the mapper
                        }
                        catch (Exception ex)
                        {
                            LogEventMappingExecutionThrewException(_logger, mapperInterface.FullName ?? "UnknownMapper", domainEventType.FullName ?? "UnknownPoco", ex);
                            throw; // Re-throw if not TargetInvocationException
                        }
                    });
                    LogEventMapperDelegateRegistered(_logger, mapperInterface.FullName ?? mapperImplType.Name, domainEventType.Name, integrationEventType.Name);
                }
            }
            _typesScannedAndMapped = true;
            LogMappingScanCompleted(_logger, _pocoDomainEventTypeMap.Count, _pocoToProtoMapperDelegates.Count);
        }
    }

    private IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            LogTypeLoadErrorDuringScan(_logger, assembly.FullName ?? "UnknownAssembly", ex);
            if (ex.LoaderExceptions != null)
            {
                foreach (var loaderEx in ex.LoaderExceptions.Where(e => e != null))
                {
                    LogIndividualLoaderExceptionDetail(_logger, assembly.FullName ?? "UnknownAssembly", loaderEx!.GetType().Name, loaderEx.Message, loaderEx);
                }
            }
            return Type.EmptyTypes;
        }
        catch (Exception ex)
        {
            LogGenericAssemblyScanError(_logger, assembly.FullName ?? "UnknownAssembly", ex);
            return Type.EmptyTypes;
        }
    }

    public async Task PublishAsync(
        string eventTypeName,
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        LogPublishAttempt(_logger, eventTypeName, payload.Length, headers.Count);

        if (!_pocoDomainEventTypeMap.TryGetValue(eventTypeName, out Type? pocoDomainEventType))
        {
            LogUnknownPocoEventTypeForPublish(_logger, eventTypeName);
            throw new ArgumentException($"Unknown POCO domain event type FQN: '{eventTypeName}'. This event type was not discovered during startup. Ensure it implements IDomainEvent and its assembly is scanned.", nameof(eventTypeName));
        }

        IDomainEvent deserializedPocoEvent;
        try
        {
            var deserializedObject = JsonSerializer.Deserialize(payload, pocoDomainEventType, _jsonSerializerOptions);
            if (deserializedObject is not IDomainEvent tempEvent) // Also handles null check
            {
                 LogDeserializationResultedInNullOrWrongType(_logger, eventTypeName, pocoDomainEventType.FullName ?? "N/A", deserializedObject?.GetType().FullName);
                 throw new InvalidOperationException($"JSON payload for POCO EventType '{eventTypeName}' deserialized to null or an incompatible type. Expected {typeof(IDomainEvent)}.");
            }
            deserializedPocoEvent = tempEvent;
        }
        catch (JsonException jsonEx)
        {
            LogDeserializationFailedDuringPublish(_logger, eventTypeName, pocoDomainEventType.FullName ?? "N/A", payload.Substring(0, Math.Min(payload.Length, 200)), jsonEx);
            throw new ArgumentException($"Failed to deserialize JSON payload for POCO EventType '{eventTypeName}' to .NET type '{pocoDomainEventType.FullName}'. Check payload structure and content. Error: {jsonEx.Message}", nameof(payload), jsonEx);
        }

        LogPocoEventDeserializedForPublish(_logger, pocoDomainEventType.FullName ?? "N/A", deserializedPocoEvent.Id, deserializedPocoEvent.OccurredOn);

        if (!_pocoToProtoMapperDelegates.TryGetValue(pocoDomainEventType, out var mapperDelegate))
        {
            LogProtobufMapperDelegateNotFoundForPublish(_logger, pocoDomainEventType.FullName ?? "N/A");
            throw new MessagingConfigurationException(
                $"Protobuf mapper delegate not found for POCO domain event type '{pocoDomainEventType.FullName}'. This is a critical configuration error; ensure an IEventMapper<TDomain, TProto> is implemented, registered in DI, and its assembly is scanned by the publisher.",
                "MAPPER_DELEGATE_MISSING");
        }

        IMessage protobufIntegrationEvent;
        Type protobufMessageType;
        try
        {
            protobufIntegrationEvent = mapperDelegate(deserializedPocoEvent, _serviceProvider); // Pass IServiceProvider
            protobufMessageType = protobufIntegrationEvent.GetType();
            LogProtobufEventMappedSuccessfully(_logger, pocoDomainEventType.FullName ?? "N/A", protobufMessageType.FullName ?? "N/A");
        }
        catch (Exception mapEx)
        {
            LogEventMappingExecutionFailedDuringPublish(_logger, pocoDomainEventType.FullName ?? "N/A", mapEx);
            throw new InvalidOperationException($"Error executing Protobuf mapper for POCO event type '{pocoDomainEventType.FullName}'. See inner exception for details.", mapEx);
        }

        LogPublishingToMassTransitEndpoint(_logger, protobufMessageType.FullName ?? "N/A", deserializedPocoEvent.Id);

        try
        {
            await _publishEndpoint.Publish(protobufIntegrationEvent, protobufMessageType, sendContext =>
            {
                sendContext.MessageId = deserializedPocoEvent.Id; // Use domain event's ID for traceability
                if (headers.TryGetValue("CorrelationId", StringComparison.OrdinalIgnoreCase) &&
                    Guid.TryParse(headers["CorrelationId"], out Guid correlationGuid))
                {
                    sendContext.CorrelationId = correlationGuid;
                }
                else if (deserializedPocoEvent is ICorrelated correlatedEvent && correlatedEvent.CorrelationId != default)
                {
                     sendContext.CorrelationId = correlatedEvent.CorrelationId;
                }
                // else, MassTransit will generate one if not set

                foreach (var headerEntry in headers.Where(h => !h.Key.Equals("CorrelationId", StringComparison.OrdinalIgnoreCase)))
                {
                    sendContext.Headers.Set(headerEntry.Key, headerEntry.Value);
                }
                LogMassTransitSendContextConfiguredForPublish(_logger, sendContext.MessageId, protobufMessageType.FullName ?? "N/A", sendContext.CorrelationId);
            }, cancellationToken).ConfigureAwait(false);

            LogPublishSucceededToMassTransit(_logger, protobufMessageType.FullName ?? "N/A", deserializedPocoEvent.Id, _publishEndpoint.GetType().Name);
        }
        catch (Exception publishEx)
        {
            LogPublishFailedToMassTransit(_logger, protobufMessageType.FullName ?? "N/A", deserializedPocoEvent.Id, publishEx);
            throw; // Re-throw to allow outbox or higher-level error handling.
        }
    }
}