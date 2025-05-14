namespace TemporaryName.Domain.Primitives.DomainEvent;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
}