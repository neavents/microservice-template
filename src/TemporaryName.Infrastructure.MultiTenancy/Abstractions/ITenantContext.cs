using System;

namespace TemporaryName.Infrastructure.MultiTenancy.Abstractions;

public interface ITenantContext
{
    public ITenantInfo? CurrentTenant { get; }
    public bool IsTenantResolvedAndActive { get; }
    public void SetCurrentTenant(ITenantInfo? tenantInfo);
}
