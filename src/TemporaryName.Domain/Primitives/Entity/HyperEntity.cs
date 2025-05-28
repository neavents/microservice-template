using System;

namespace TemporaryName.Domain.Primitives.Entity;

public class HyperEntity<TId> : AuditedEntity<TId>
where TId : IEquatable<TId>
{
    
}
