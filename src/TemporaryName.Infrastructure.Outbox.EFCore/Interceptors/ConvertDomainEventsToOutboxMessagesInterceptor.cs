// src/TemporaryName.Infrastructure.Outbox.EFCore/Interceptors/ConvertDomainEventsToOutboxMessagesInterceptor.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking; // For EntityEntry
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics; // For Activity
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TemporaryName.Domain.Primitives; // For IIdentifiable, if used
using TemporaryName.Domain.Primitives.AggregateRoot;
using TemporaryName.Domain.Primitives.DomainEvent;
using TemporaryName.Infrastructure.Outbox.EFCore.Entities; // Using the EF Core Entity
// Assuming your contextual interfaces are defined in a known location:
// using TemporaryName.Domain.Primitives.Contextual;

namespace TemporaryName.Infrastructure.Outbox.EFCore.Interceptors;

// Define these interfaces or ensure they exist in your Domain project, e.g., in TemporaryName.Domain.Primitives.Contextual
public interface IVersionable { long Version { get; } }
public interface ICorrelated { Guid? CorrelationId { get; } }
public interface ICaused { Guid? CausationId { get; } }
public interface IUserContextualInfoProvider { string? UserId { get; } } // Event carries user info
public interface ITenantedInfoProvider { string? TenantId { get; } }     // Event carries tenant info
public interface IContainsCustomMetadata { IReadOnlyDictionary<string, object> GetCustomMetadata(); }


/// <summary>
/// EF Core SaveChangesInterceptor that captures domain events from entities implementing IAggregateRoot
/// and converts them into OutboxMessage entities. These OutboxMessages are then added to the same
/// DbContext transaction, ensuring atomic persistence of business state changes and their corresponding events.
/// This is a cornerstone of the Transactional Outbox pattern for reliable event publishing.
/// </summary>
public sealed class ConvertDomainEventsToOutboxMessagesInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<ConvertDomainEventsToOutboxMessagesInterceptor> _logger;
    private const string DEFAULT_AGGREGATE_ID = "UNKNOWN_AGGREGATE_ID";
    private const string DEFAULT_PROTO_SCHEMA_VERSION = "1.0"; // Should match your OutboxMessageProto definition

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = false, // Compact for DB storage
        // PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Uncomment if domain events use camelCase
        // DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull, // Optional
        // Consider adding custom converters for complex ValueObjects within domain events if needed.
    };

    public ConvertDomainEventsToOutboxMessagesInterceptor(ILogger<ConvertDomainEventsToOutboxMessagesInterceptor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        ProcessAndPersistDomainEventsAsOutboxMessages(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        ProcessAndPersistDomainEventsAsOutboxMessages(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ProcessAndPersistDomainEventsAsOutboxMessages(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            _logger.LogWarning("DbContext is null in ProcessAndPersistDomainEventsAsOutboxMessages. Cannot capture domain events.");
            return;
        }

        // Efficiently find aggregate roots that have uncommitted domain events.
        List<IAggregateRoot> aggregateRootsWithEvents = dbContext.ChangeTracker
            .Entries<IAggregateRoot>() // Tracks entities that implement IAggregateRoot
            .Select(entry => entry.Entity)
            .Where(aggregateRoot => aggregateRoot.GetDomainEvents().Any()) // Check if there are any events
            .ToList();

        if (!aggregateRootsWithEvents.Any())
        {
            _logger.LogTrace("No domain events found in tracked aggregate roots during SaveChanges.");
            return; // No domain events to process
        }

        List<OutboxMessage> outboxMessagesToPersist = new List<OutboxMessage>();
        Activity? currentActivity = Activity.Current; // Capture current activity once for tracing context

        foreach (IAggregateRoot aggregateRoot in aggregateRootsWithEvents)
        {
            IReadOnlyCollection<IDomainEvent> domainEvents = aggregateRoot.GetDomainEvents();
            if (!domainEvents.Any()) continue; // Should be redundant due to earlier filter, but defensive.

            string aggregateType = GetFormattedAggregateType(aggregateRoot);
            string aggregateId = GetFormattedAggregateId(aggregateRoot);
            long? aggregateVersion = (aggregateRoot is IVersionable versionedAggregate) ? versionedAggregate.Version : null;

            foreach (IDomainEvent domainEvent in domainEvents)
            {
                try
                {
                    // --- Core Event Data ---
                    Guid eventId = domainEvent.Id; // Assuming IDomainEvent has Guid Id
                    DateTimeOffset occurredAtUtc = domainEvent.OccurredOn; // Assuming IDomainEvent has DateTime OccurredOn (UTC)

                    string eventTypeFqn = domainEvent.GetType().FullName ?? domainEvent.GetType().Name;

                    // Serialize the domain event payload to JSON.
                    // Pass domainEvent.GetType() to ensure polymorphic serialization if events have inheritance.
                    string payloadJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _jsonSerializerOptions);

                    // --- Contextual Data Extraction ---
                    Guid? correlationId = ExtractCorrelationIdFromActivityOrEvent(currentActivity, domainEvent);
                    Guid? causationId = ExtractCausationIdFromEvent(domainEvent);
                    string? userId = ExtractUserIdFromEventOrContext(domainEvent, dbContext);
                    string? tenantId = ExtractTenantIdFromEventOrContext(domainEvent, dbContext);

                    string? traceContextJson = SerializeTraceContext(currentActivity);
                    string? metadataJson = SerializeCustomMetadataFromEvent(domainEvent);

                    var outboxMessage = new OutboxMessage(
                        eventId: eventId,
                        eventTypeFqn: eventTypeFqn,
                        payloadJson: payloadJson,
                        occurredAtUtc: occurredAtUtc,
                        aggregateType: aggregateType,
                        aggregateId: aggregateId,
                        aggregateVersion: aggregateVersion,
                        correlationId: correlationId,
                        causationId: causationId,
                        userId: userId,
                        tenantId: tenantId,
                        traceContextJson: traceContextJson,
                        protoSchemaVersion: DEFAULT_PROTO_SCHEMA_VERSION, // This should align with your current outbox_message.v1.proto
                        metadataJson: metadataJson
                    );
                    outboxMessagesToPersist.Add(outboxMessage);

                    _logger.LogDebug("Domain Event {EventId} (Type: {EventType}) converted to OutboxMessage for Aggregate {AggregateType}:{AggregateId}.",
                                     eventId, eventTypeFqn, aggregateType, aggregateId);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogCritical(jsonEx, "CRITICAL: JSON Serialization Error for domain event {DomainEventType} (ID: {EventId}) for aggregate {AggregateType}:{AggregateId}. This event will NOT be outboxed, and the transaction might fail depending on overall error handling. Payload: {PayloadDebug}",
                                     domainEvent.GetType().FullName, domainEvent.Id, aggregateType, aggregateId, domainEvent.ToString()); // Log minimal payload info for debugging
                    // For FAANG level, this should likely cause the SaveChanges to fail to prevent inconsistent state.
                    // Re-throwing here will typically abort the SaveChanges operation.
                    throw new InvalidOperationException($"Failed to serialize domain event {domainEvent.GetType().FullName} (ID: {domainEvent.Id}) to JSON for outbox. See inner exception for details.", jsonEx);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "CRITICAL: Unexpected error processing domain event {DomainEventType} (ID: {EventId}) for outbox. Aggregate {AggregateType}:{AggregateId}. This event may not be outboxed, and the transaction might fail.",
                                     domainEvent.GetType().FullName, domainEvent.Id, aggregateType, aggregateId);
                    throw new InvalidOperationException($"Unexpected error processing domain event {domainEvent.GetType().FullName} (ID: {domainEvent.Id}) for outbox. See inner exception for details.", ex);
                }
            }
            aggregateRoot.ClearDomainEvents(); // Crucial: Clear events from the aggregate after they've been processed for outboxing.
        }

        if (outboxMessagesToPersist.Any())
        {
            // Add all prepared OutboxMessage entities to the DbContext.
            // They will be saved as part of the same transaction as the business data.
            dbContext.Set<OutboxMessage>().AddRange(outboxMessagesToPersist);
            _logger.LogInformation("{OutboxMessageCount} OutboxMessage(s) prepared and added to DbContext ChangeTracker for persistence.", outboxMessagesToPersist.Count);
        }
    }

    private string GetFormattedAggregateType(IAggregateRoot aggregateRoot)
    {
        string typeName = aggregateRoot.GetType().Name;
        const string suffix = "Aggregate";
        // Conventionally remove "Aggregate" suffix for a cleaner type name if present.
        return typeName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? typeName.Substring(0, typeName.Length - suffix.Length)
            : typeName;
    }

    private string GetFormattedAggregateId(IAggregateRoot aggregateRoot)
    {
        // Your robust IIdentifiable<T> based logic from your version
        if (aggregateRoot is IIdentifiable<Guid> guidId) return guidId.Id.ToString();
        if (aggregateRoot is IIdentifiable<int> intId) return intId.Id.ToString(CultureInfo.InvariantCulture);
        if (aggregateRoot is IIdentifiable<long> longId) return longId.Id.ToString(CultureInfo.InvariantCulture);
        if (aggregateRoot is IIdentifiable<string> stringId) return stringId.Id;
        if (aggregateRoot is IIdentifiable<decimal> decimalId) return decimalId.Id.ToString(CultureInfo.InvariantCulture);

        // Fallback for other ID types or if IIdentifiable is not used consistently.
        // Consider a common base or interface for all aggregate IDs if possible.
        // PropertyInfo? idProp = aggregateRoot.GetType().GetProperty("Id");
        // if (idProp != null && idProp.GetValue(aggregateRoot) is object idValue)
        // {
        //     return idValue.ToString() ?? DEFAULT_AGGREGATE_ID;
        // }

        _logger.LogWarning("Could not determine a string representation for AggregateId of type {AggregateActualType}. Using default: {DefaultId}.",
            aggregateRoot.GetType().FullName, DEFAULT_AGGREGATE_ID);
        return DEFAULT_AGGREGATE_ID;
    }

    private Guid? ExtractCorrelationIdFromActivityOrEvent(Activity? activity, IDomainEvent domainEvent)
    {
        string? correlationStr = (string?)(activity?.GetBaggageItem("CorrelationId") ?? activity?.GetTagItem("CorrelationId"));
        if (Guid.TryParse(correlationStr, out Guid parsedFromActivity))
        {
            _logger.LogTrace("Extracted CorrelationId '{CorrelationId}' from current Activity for EventId {EventId}.", parsedFromActivity, domainEvent.Id);
            return parsedFromActivity;
        }
        if (domainEvent is ICorrelated correlatedEvent && correlatedEvent.CorrelationId.HasValue)
        {
            _logger.LogTrace("Extracted CorrelationId '{CorrelationId}' from ICorrelated DomainEvent for EventId {EventId}.", correlatedEvent.CorrelationId.Value, domainEvent.Id);
            return correlatedEvent.CorrelationId;
        }
        _logger.LogTrace("CorrelationId not found for EventId {EventId}.", domainEvent.Id);
        return null;
    }

    private Guid? ExtractCausationIdFromEvent(IDomainEvent domainEvent)
    {
        if (domainEvent is ICaused causedEvent && causedEvent.CausationId.HasValue)
        {
            _logger.LogTrace("Extracted CausationId '{CausationId}' from ICaused DomainEvent for EventId {EventId}.", causedEvent.CausationId.Value, domainEvent.Id);
            return causedEvent.CausationId;
        }
        _logger.LogTrace("CausationId not found for EventId {EventId}.", domainEvent.Id);
        return null;
    }

    private string? ExtractUserIdFromEventOrContext(IDomainEvent domainEvent, DbContext? dbContext)
    {
        if (domainEvent is IUserContextualInfoProvider userEvent && !string.IsNullOrWhiteSpace(userEvent.UserId))
        {
             _logger.LogTrace("Extracted UserId '{UserId}' from IUserContextualInfoProvider DomainEvent for EventId {EventId}.", userEvent.UserId, domainEvent.Id);
            return userEvent.UserId;
        }
        // Placeholder for extracting from an ambient context if available and safe.
        // Example: (dbContext as IAppUserContextProvider)?.GetCurrentUserId();
        // Avoid service locator patterns here. Prefer event enrichment earlier in the flow.
        _logger.LogTrace("UserId not found for EventId {EventId}.", domainEvent.Id);
        return null;
    }

    private string? ExtractTenantIdFromEventOrContext(IDomainEvent domainEvent, DbContext? dbContext)
    {
        if (domainEvent is ITenantedInfoProvider tenantedEvent && !string.IsNullOrWhiteSpace(tenantedEvent.TenantId))
        {
            _logger.LogTrace("Extracted TenantId '{TenantId}' from ITenantedInfoProvider DomainEvent for EventId {EventId}.", tenantedEvent.TenantId, domainEvent.Id);
            return tenantedEvent.TenantId;
        }
        // Placeholder for ambient context
        _logger.LogTrace("TenantId not found for EventId {EventId}.", domainEvent.Id);
        return null;
    }

    private string? SerializeTraceContext(Activity? activity)
    {
        if (activity?.IdFormat == ActivityIdFormat.W3C && activity.Id != null)
        {
            Dictionary<string, string> traceContextMap = new Dictionary<string, string> { { "traceparent", activity.Id } };
            if (activity.TraceStateString != null)
            {
                traceContextMap["tracestate"] = activity.TraceStateString;
            }
            try
            {
                return JsonSerializer.Serialize(traceContextMap, _jsonSerializerOptions);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx, "Failed to serialize trace context from Activity Id {ActivityId}. Trace context will be omitted from outbox message.", activity.Id);
            }
        }
        return null;
    }

    private string? SerializeCustomMetadataFromEvent(IDomainEvent domainEvent)
    {
        if (domainEvent is IContainsCustomMetadata metadataEvent)
        {
            IReadOnlyDictionary<string, object> metadata = metadataEvent.GetCustomMetadata();
            if (metadata.Any())
            {
                try
                {
                    return JsonSerializer.Serialize(metadata, _jsonSerializerOptions);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogWarning(jsonEx, "Failed to serialize custom metadata for domain event {DomainEventType} (ID: {EventId}). Metadata will be omitted.",
                                         domainEvent.GetType().FullName, domainEvent.Id);
                }
            }
        }
        return null;
    }
}
