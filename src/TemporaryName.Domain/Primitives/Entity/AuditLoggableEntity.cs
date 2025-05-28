using System;
using TemporaryName.Domain.Primitives.Log;

namespace TemporaryName.Domain.Primitives.Entity;

public class AuditLoggableEntity<TId> : AuditedEntity<TId>, IAuditLoggable
where TId : IEquatable<TId>
{
    public object GetBusinessIdentifier()
    {
        throw new NotImplementedException();
    }

    public string GetEntityTypeForAudit()
    {
        throw new NotImplementedException();
    }
}
