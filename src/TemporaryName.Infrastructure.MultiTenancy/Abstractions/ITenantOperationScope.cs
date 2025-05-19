using System;

namespace TemporaryName.Infrastructure.MultiTenancy.Abstractions;

public interface ITenantOperationScope : IDisposable
{
    ITenantInfo? ActiveTenantInfo { get; }
}
