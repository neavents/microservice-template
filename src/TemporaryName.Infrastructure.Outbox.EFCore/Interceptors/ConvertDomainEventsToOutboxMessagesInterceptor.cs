using System;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using TemporaryName.Domain.Primitives;
using TemporaryName.Domain.Primitives.AggregateRoot;
using TemporaryName.Domain.Primitives.DomainEvent;
using TemporaryName.Domain.Primitives.Entity;
using TemporaryName.Domain.Primitives.Outbox;

namespace TemporaryName.Infrastructure.Outbox.EFCore.Interceptors;

public class ConvertDomainEventsToOutboxMessagesInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<ConvertDomainEventsToOutboxMessagesInterceptor> _logger;
    private const string DEFAULT_ID = "UNKNOWN_ID";
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = false, // Compact for DB storage
        // Add any custom converters if your domain events need them
    };

    public ConvertDomainEventsToOutboxMessagesInterceptor(ILogger<ConvertDomainEventsToOutboxMessagesInterceptor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

        public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        ProcessOutboxMessages(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        ProcessOutboxMessages(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ProcessOutboxMessages(DbContext? dbContext)
    {
        if (dbContext is null) return;

        var aggregateRoots = dbContext.ChangeTracker
            .Entries<IAggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggRoot => aggRoot.GetDomainEvents().Count != 0)
            .ToList();

        if (aggregateRoots.Count == 0) return;

        List<OutboxMessage> outboxMessages = new List<OutboxMessage>();

        foreach (IAggregateRoot aggregateRoot in aggregateRoots)
        {
            IReadOnlyCollection<IDomainEvent> domainEvents = aggregateRoot.GetDomainEvents();
            string aggregateType = aggregateRoot.GetType().Name;
            // Assuming your AggregateRoot.Id is accessible and convertible to string.
            // You might need a common interface IIdentifiable<T> or similar.
            // For now, let's assume aggregateRoot.Id.ToString() works or you have a method.
            // If AggregateRootId is a ValueObject, it should have a .Value property.
            string aggregateIdValue = DEFAULT_ID; // Default if ID cannot be resolved
            if (aggregateRoot is IIdentifiable<Guid> entityWithGuidId) // Example if your IAggregateRoot implements IEntity<Guid>
            {
                aggregateIdValue = entityWithGuidId.Id.ToString();
            } else if (aggregateRoot is IIdentifiable<int> entityWithIntId) // Example if your IAggregateRoot implements IEntity<Guid>
            {
                aggregateIdValue = entityWithIntId.Id.ToString(CultureInfo.InvariantCulture);
            } else if (aggregateRoot is IIdentifiable<long> entityWithLongId) // Example if your IAggregateRoot implements IEntity<Guid>
            {
                aggregateIdValue = entityWithLongId.Id.ToString(CultureInfo.InvariantCulture);
            } else if (aggregateRoot is IIdentifiable<string> entityWithStringId) // Example if your IAggregateRoot implements IEntity<Guid>
            {
                aggregateIdValue = entityWithStringId.Id;
            } else if (aggregateRoot is IIdentifiable<decimal> entityWithDecimalId) // Example if your IAggregateRoot implements IEntity<Guid>
            {
                aggregateIdValue = entityWithDecimalId.Id.ToString(CultureInfo.InvariantCulture);
            }
            // else if (aggregateRoot.Id is AggregateRootId<Guid> typedId) // Your specific AggregateRootId
            // {
            //     aggregateIdValue = typedId.Value.ToString();
            // }


            foreach (IDomainEvent domainEvent in domainEvents)
            {
                try
                {
                    string eventType = domainEvent.GetType().FullName ?? domainEvent.GetType().Name;
                    string payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _jsonSerializerOptions);

                    outboxMessages.Add(new OutboxMessage(
                        aggregateType,
                        aggregateIdValue,
                        eventType,
                        payload
                        // Consider adding CorrelationId if available from domainEvent or context
                    ));
                    _logger.LogDebug("Domain Event {EventType} converted to OutboxMessage for Aggregate {AggregateType}:{AggregateId}",
                                     eventType, aggregateType, aggregateIdValue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error serializing domain event {DomainEventType} for outbox.", domainEvent.GetType().Name);
                    // Potentially throw or handle more gracefully depending on requirements
                }
            }
            aggregateRoot.ClearDomainEvents();
        }

        if (outboxMessages.Count != 0)
        {
            dbContext.Set<OutboxMessage>().AddRange(outboxMessages);
            _logger.LogInformation("{OutboxMessageCount} OutboxMessages added to DbContext.", outboxMessages.Count);
        }
    }
}
