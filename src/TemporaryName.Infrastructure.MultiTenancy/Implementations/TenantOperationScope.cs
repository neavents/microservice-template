using System;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations;

public partial class TenantOperationScope : ITenantOperationScope
{ 
    public ITenantInfo? ActiveTenantInfo { get; }
    private readonly ITenantInfo? _previousTenantInfo;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger _logger;
    private bool _disposed;

    public TenantOperationScope(ITenantInfo activeTenantInfo, ITenantInfo? previousTenantInfo, ITenantContext tenantContext, ILogger logger)
    {
        ActiveTenantInfo = activeTenantInfo;
        _previousTenantInfo = previousTenantInfo;
        _tenantContext = tenantContext;
        _logger = logger;
    }
    public void Dispose()
    {
        if (!_disposed)
        {
            _tenantContext.SetCurrentTenant(_previousTenantInfo);
            LogOperationScopeDisposed(_logger, _previousTenantInfo?.Id ?? "null", ActiveTenantInfo?.Id ?? "N/A");
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
