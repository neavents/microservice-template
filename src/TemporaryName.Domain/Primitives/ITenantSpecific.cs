using System;

namespace TemporaryName.Domain.Primitives;

public interface ITenantSpecific
{
    public Guid TenantId { get; }
}
