using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf; // For IMessage
using TemporaryName.Application.Contracts.Abstractions.Messaging;
using TemporaryName.Domain.Primitives.DomainEvent;
// Example: Your POCO Domain Event
// namespace TemporaryName.Domain.Orders.Events { public record OrderCreatedDomainEvent(string OrderId, string CustomerId, DateTime OrderDate, double TotalAmount) : IDomainEvent; }
// Example: Your Protobuf Integration Event (generated from .proto)
// using TemporaryName.SharedContracts.Protobuf.IntegrationEvents.V1;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Publishers;

public class MassTransitIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MassTransitIntegrationEventPublisher> _logger;
    private readonly ConcurrentDictionary<string, Type> _domainEventTypeMap; // Maps FQN string to POCO DomainEvent Type
    private readonly ConcurrentDictionary<Type, Func<IDomainEvent, IMessage>> _pocoToProtoMappers; // Maps POCO Type to a Protobuf mapping function

    private static bool _typesScannedAndMapped = false;
    private static readonly object _scanLock = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public MassTransitIntegrationEventPublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<MassTransitIntegrationEventPublisher> logger,
        IServiceProvider serviceProvider) // For resolving mappers if registered in DI
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _domainEventTypeMap = new ConcurrentDictionary<string, Type>();
        _pocoToProtoMappers = new ConcurrentDictionary<Type, Func<IDomainEvent, IMessage>>();
        _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        ScanAndMapEventTypesOnce(serviceProvider);
    }

    // Example POCO Domain Event (adjust to your actual domain events)
    // This should ideally be in your Domain layer or Application.Contracts if it's a DTO.
    // For this example, I'll define a conceptual one.
    // Assume: namespace TemporaryName.Domain.Events { public record SampleOrderCreatedDomainEvent(string OrderId, string CustomerId, DateTime Timestamp, decimal Amount) : IDomainEvent; }

    private void ScanAndMapEventTypesOnce(IServiceProvider serviceProvider)
    {
        if (_typesScannedAndMapped) return;
        lock (_scanLock)
        {
            if (_typesScannedAndMapped) return;

            _logger.LogInformation("MassTransitIntegrationEventPublisher: Scanning for IDomainEvent POCOs and their Protobuf mappers...");

            // 1. Scan for IDomainEvent implementations (POCOs)
            // (Same scanning logic as before for _domainEventTypeMap)
            List<Type> domainEventPocoTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => !asm.IsDynamic &&
                              (asm.FullName?.StartsWith("TemporaryName.Domain", StringComparison.OrdinalIgnoreCase) == true ||
                               asm.FullName?.StartsWith("TemporaryName.Application", StringComparison.OrdinalIgnoreCase) == true || // Include Application for event DTOs
                               asm.FullName?.StartsWith(Assembly.GetEntryAssembly()?.GetName().Name ?? "ENTRY_ASSEMBLY_FALLBACK", StringComparison.OrdinalIgnoreCase) == true))
                .SelectMany(SafeGetTypes)
                .Where(t => t != null && typeof(IDomainEvent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && !typeof(IMessage).IsAssignableFrom(t)) // Ensure it's not already a Proto message
                .ToList();

            foreach (Type pocoType in domainEventPocoTypes)
            {
                if (pocoType.FullName != null)
                {
                    _domainEventTypeMap.TryAdd(pocoType.FullName, pocoType);
                    _logger.LogDebug("Mapped POCO Event FQN '{PocoFqn}' to .NET Type '{PocoType}'.", pocoType.FullName, pocoType.Name);

                    // 2. Discover and register mappers (POCO DomainEvent -> Protobuf IntegrationEvent)
                    // Convention: Look for a static `ToIntegrationEventProto` method on the POCO,
                    // or a dedicated mapper class resolvable via IServiceProvider, or attribute-based.
                    // For simplicity, let's assume a convention or manual registration here.
                    // Example: Manually register a mapper for a specific POCO event to its Protobuf counterpart
                    if (pocoType.Name == "OrderCreatedDomainEvent") // Replace with actual POCO event type
                    {
                        // This assumes you have a POCO OrderCreatedDomainEvent and a corresponding
                        // TemporaryName.SharedContracts.Protobuf.IntegrationEvents.V1.OrderCreatedIntegrationEvent
                        _pocoToProtoMappers.TryAdd(pocoType, domainEvent =>
                        {
                            // var poco = (TemporaryName.Domain.Orders.Events.OrderCreatedDomainEvent)domainEvent; // Cast to actual POCO type
                            // return new TemporaryName.SharedContracts.Protobuf.IntegrationEvents.V1.OrderCreatedIntegrationEvent
                            // {
                            // OrderId = poco.OrderId,
                            // CustomerId = poco.CustomerId,
                            // OrderDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(poco.OrderDate.ToUniversalTime()),
                            // TotalAmountValue = (double)poco.TotalAmount // Example conversion
                            // Add items mapping if applicable
                            // };
                            _logger.LogWarning("Placeholder mapper used for {PocoType}. Implement actual mapping.", pocoType.FullName);
                            // Fallback to a generic wrapper if no specific mapper (NOT recommended for production)
                            // This requires a generic Protobuf message like GenericIntegrationEvent { string type; string data; }
                            // For robust typing, explicit mappers are key.
                            throw new NotImplementedException($"Mapper not implemented for {pocoType.FullName} to its Protobuf integration event.");
                        });
                         _logger.LogInformation("Registered placeholder Protobuf mapper for POCO type {PocoType}", pocoType.Name);
                    }
                    // You would need to add mappers for all your domain events that go on the bus.
                    // These could be discovered via reflection for classes implementing IEventMapper<TDomainEvent, TProtoEvent>
                    // and resolved using IServiceProvider.
                }
            }
            _typesScannedAndMapped = true;
            _logger.LogInformation("Finished scanning for POCO events and mappers. Found {PocoCount} POCO event types.", _domainEventTypeMap.Count);
        }
    }

    private IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex)
        {
            _logger.LogWarning("Could not load types from {AssemblyName}: {LoaderErrors}",
                assembly.FullName, string.Join(", ", ex.LoaderExceptions.Select(e => e?.Message)));
            return Type.EmptyTypes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting types from assembly {AssemblyName}", assembly.FullName);
            return Type.EmptyTypes;
        }
    }


    public async Task PublishAsync(
        string eventTypeName, // This is the FQN of the POCO Domain Event
        string payload,       // JSON of the POCO Domain Event
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        if (!_domainEventTypeMap.TryGetValue(eventTypeName, out Type? pocoDomainEventType))
        {
            _logger.LogError("Unknown POCO domain event type string '{EventTypeFqn}'. Cannot map to .NET Type for JSON deserialization.", eventTypeName);
            throw new InvalidOperationException($"Unmapped POCO domain event type FQN: {eventTypeName}.");
        }

        IDomainEvent? deserializedPocoEvent;
        try
        {
            deserializedPocoEvent = JsonSerializer.Deserialize(payload, pocoDomainEventType, _jsonSerializerOptions) as IDomainEvent;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Failed to deserialize JSON payload for POCO EventType '{EventTypeFqn}' to .NET type '{NetType}'.",
                             eventTypeName, pocoDomainEventType.FullName);
            throw;
        }

        if (deserializedPocoEvent is null)
        {
            _logger.LogError("JSON payload for POCO EventType '{EventTypeFqn}' deserialized to null.", eventTypeName);
            throw new InvalidOperationException($"Deserialized POCO domain event was null for type {pocoDomainEventType.FullName}.");
        }

        // Now, map the POCO domain event to its corresponding Protobuf IMessage
        if (!_pocoToProtoMappers.TryGetValue(pocoDomainEventType, out Func<IDomainEvent, IMessage>? mapperFunc))
        {
            _logger.LogError("No Protobuf mapper registered for POCO domain event type '{PocoType}'. Cannot publish to MassTransit with Protobuf.", pocoDomainEventType.FullName);
            throw new InvalidOperationException($"Protobuf mapper not found for {pocoDomainEventType.FullName}.");
        }

        IMessage protobufIntegrationEvent = mapperFunc(deserializedPocoEvent);
        Type protobufMessageType = protobufIntegrationEvent.GetType(); // Get the actual Protobuf message type

        _logger.LogInformation("Publishing Protobuf integration event of type {ProtoType} (mapped from POCO {PocoType}) via MassTransit...",
                             protobufMessageType.FullName, pocoDomainEventType.FullName);

        // MassTransit will use its configured Protobuf serializer for 'protobufIntegrationEvent'.
        await _publishEndpoint.Publish(protobufIntegrationEvent, protobufMessageType, sendContext =>
        {
            foreach (KeyValuePair<string, string> headerEntry in headers)
            {
                if (headerEntry.Key.Equals("CorrelationId", StringComparison.OrdinalIgnoreCase) &&
                    Guid.TryParse(headerEntry.Value, out Guid correlationGuid))
                {
                    sendContext.CorrelationId = correlationGuid;
                }
                else
                {
                    sendContext.Headers.Set(headerEntry.Key, headerEntry.Value);
                }
            }
            // Set MessageId from original event if available and desired, or let MassTransit generate one.
            // if (headers.TryGetValue("X-Original-Event-Id", out string originalEventId) && Guid.TryParse(originalEventId, out Guid eventIdGuid))
            // {
            //    sendContext.MessageId = eventIdGuid;
            // }

            _logger.LogDebug("Publishing Protobuf event {EventId} of type {ProtoType} with CorrelationId {CorrelationId}",
                sendContext.MessageId, protobufMessageType.FullName, sendContext.CorrelationId);

        }, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Protobuf Integration Event {ProtoType} published to MassTransit.", protobufMessageType.FullName);
    }
}