using System;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations;

public partial class TenantContext : ITenantContext
{
    private static readonly AsyncLocal<ITenantInfo?> _currentTenantAsyncLocal = new();
    private readonly ILogger<TenantContext> _logger;

    public TenantContext(ILogger<TenantContext> logger)
    {
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _logger = logger;
    }

    public ITenantInfo? CurrentTenant => _currentTenantAsyncLocal.Value;

    public bool IsTenantResolvedAndActive => CurrentTenant != null && CurrentTenant.Status == TenantStatus.Active;

    public void SetCurrentTenant(ITenantInfo? tenantInfo)
    {
        ITenantInfo? previousTenant = _currentTenantAsyncLocal.Value;

        if (previousTenant is not null && tenantInfo is not null && previousTenant.Id != tenantInfo.Id)
        {
            LogOverwritingTenantContext(_logger, previousTenant.Id, tenantInfo.Id);
        }
        else if (previousTenant is not null && tenantInfo is null)
        {
            LogClearingTenantContext(_logger, previousTenant.Id);
        }

        _currentTenantAsyncLocal.Value = tenantInfo;

        if (tenantInfo is not null)
        {
            LogCurrentTenantSet(_logger, tenantInfo.Id, tenantInfo.Status, IsTenantResolvedAndActive);

            if (tenantInfo.Status != TenantStatus.Active)
            {
                LogTenantResolvedButNotActive(_logger, tenantInfo.Id, tenantInfo.Status);
            }
        }
        else
        {
            LogTenantSetToNull(_logger);
        }
    }
}
