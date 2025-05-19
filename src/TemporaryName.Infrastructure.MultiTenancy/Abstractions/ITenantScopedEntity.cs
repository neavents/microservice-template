using System;

namespace TemporaryName.Infrastructure.MultiTenancy.Abstractions;

public interface ITenantScopedEntity
{
    public string TenantId { get; set; } 
}
