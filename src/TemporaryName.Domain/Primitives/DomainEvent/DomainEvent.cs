namespace TemporaryName.Domain.Primitives.DomainEvent;

public abstract record DomainEvent : IDomainEvent
{
    public Guid Id { get; }
    public DateTimeOffset OccurredOn { get; }

    protected DomainEvent()
    {
        this.Id = Guid.NewGuid();
        this.OccurredOn = DateTimeOffset.UtcNow;
    }

    protected DomainEvent(Guid eventId, DateTimeOffset occurredOn)
    {
        this.Id = eventId;
        this.OccurredOn = occurredOn;
    }
}