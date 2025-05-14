using System;

namespace TemporaryName.Domain.Primitives.Audit;

public interface IProducesAuditTrail<TAuditEntry> where TAuditEntry : IAuditLogEntry
{
    //This is a marker interface to infrom system who implements this has producing audit log entries.
}
