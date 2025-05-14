using TemporaryName.Domain.Primitives.Entity;
using System;
using System.Text.Json;

namespace TemporaryName.Domain.Primitives.Outbox;

public class OutboxMessage : Entity<Guid> 
{
    public string EventType { get; private set; }
    public string Payload { get; private set; }
    public DateTime OccurredOnUtc { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }
    public OutboxMessageStatus Status { get; private set; }
    public string? ErrorDetails { get; private set; }
    public int RetryCount {get; private set;}
    public Guid? CorrelationId { get; private set; }
    public string AggregateType { get; private set; }
    public string AggregateId { get; private set; }

    private OutboxMessage() : base(Guid.NewGuid())
    {
        EventType = string.Empty;
        Payload = string.Empty;
        AggregateType = string.Empty;
        AggregateId = string.Empty;
    }

    public OutboxMessage(
        string aggregateType,
        string aggregateId,
        string eventType,
        string payload,
        Guid? correlationId = null) : base(Guid.NewGuid())
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(AggregateId);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(payload);
        
        AggregateType = aggregateType;
        AggregateId = aggregateId;
        EventType = eventType;
        Payload = payload;
        OccurredOnUtc = DateTime.UtcNow;
        Status = OutboxMessageStatus.Pending;
        CorrelationId = correlationId;
    }

    public void MarkAsProcessed()
    {
        Status = OutboxMessageStatus.ProcessedByConsumer;
        ProcessedOnUtc = DateTime.UtcNow;
        ErrorDetails = null;
    }

    public void MarkAsRequeued(){
        Status = OutboxMessageStatus.Requeued;
        ProcessedOnUtc = DateTime.UtcNow;
        RetryCount ++;
        ErrorDetails = null;
    }

    public void MarkAsPublished(){
        Status = OutboxMessageStatus.Published;
        ProcessedOnUtc = DateTime.UtcNow;
        ErrorDetails = null;
    }

    public void MarkAsFailed(string error)
    {
        Status = OutboxMessageStatus.FailedToPublish;
        ProcessedOnUtc = DateTime.UtcNow;
        ErrorDetails = error;
    }
}