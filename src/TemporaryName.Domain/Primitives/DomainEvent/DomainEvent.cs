namespace TemporaryName.Domain.Primitives.DomainEvent;

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }

    protected DomainEvent()
    {
        this.EventId = Guid.NewGuid();
        this.OccurredOn = DateTimeOffset.UtcNow;
    }

    protected DomainEvent(Guid eventId, DateTimeOffset occurredOn)
    {
        this.EventId = eventId;
        this.OccurredOn = occurredOn;
    }
}