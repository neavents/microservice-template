namespace TemporaryName.Domain.Primitives.DomainEvent;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredOn { get; }
}