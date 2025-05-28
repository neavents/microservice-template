using System;

namespace TemporaryName.Infrastructure.MultiTenancy.Abstractions;

public interface ITenantStore
{
    public Task<ITenantInfo?> GetTenantByIdentifierAsync(string id);
}
