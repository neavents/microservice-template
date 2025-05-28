using System;

namespace TemporaryName.Infrastructure.MultiTenancy.Abstractions;

public interface ITenantOperationScopeFactory
{
    public Task<ITenantOperationScope> CreateScopeAsync(string tenantId);
    public ITenantOperationScope CreateScope(ITenantInfo tenantInfo);
}
