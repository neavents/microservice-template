using System;
using System.Collections.Generic;
using TemporaryName.Domain.Primitives.Entity;

namespace TemporaryName.Domain.Primitives.AggregateRoot;

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : IEquatable<TId>
{
    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot() { }
}