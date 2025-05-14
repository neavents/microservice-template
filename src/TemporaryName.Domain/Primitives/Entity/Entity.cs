using System.Diagnostics.CodeAnalysis;
using TemporaryName.Domain.Primitives.DomainEvent;

namespace TemporaryName.Domain.Primitives.Entity;

public abstract class Entity<TId> : IEntity, IEquatable<Entity<TId>>, IIdentifiable<TId>
    where TId : IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public TId Id { get; protected init; }

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    protected Entity(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        this.Id = id;
    }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Entity<TId> other && this.Equals(other);
    }

    public virtual bool Equals([NotNullWhen(true)] Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (this.GetType() != other.GetType())
        {
            return false;
        }

        if (EqualityComparer<TId>.Default.Equals(this.Id, default) || EqualityComparer<TId>.Default.Equals(other.Id, default))
        {
             return false;
        }

        return EqualityComparer<TId>.Default.Equals(this.Id, other.Id);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<TId>.Default.GetHashCode(this.Id);
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }

    protected Entity() { }
}