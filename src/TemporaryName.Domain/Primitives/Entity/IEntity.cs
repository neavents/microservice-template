using TemporaryName.Domain.Primitives.DomainEvent;

namespace TemporaryName.Domain.Primitives.Entity;

public interface IEntity
{
    IReadOnlyCollection<IDomainEvent> GetDomainEvents();
    void ClearDomainEvents();
}