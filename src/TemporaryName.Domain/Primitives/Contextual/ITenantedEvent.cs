using System;

namespace TemporaryName.Domain.Primitives.Contextual;

public interface ITenantedEvent
{
    string? TenantId { get; }
}
