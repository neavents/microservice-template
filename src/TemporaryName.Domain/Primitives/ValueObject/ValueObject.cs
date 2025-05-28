using System.Diagnostics.CodeAnalysis;

namespace TemporaryName.Domain.Primitives.ValueObject;
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is ValueObject other && this.Equals(other);
    }

    public virtual bool Equals([NotNullWhen(true)] ValueObject? other)
    {
        if (other is null || this.GetType() != other.GetType())
        {
            return false;
        }

        return this.GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        HashCode hashCode = new HashCode();
        foreach (object? component in GetEqualityComponents())
        {
            hashCode.Add(component);
        }
        return hashCode.ToHashCode();
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
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

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}