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

        if (previousTenant != null && tenantInfo != null && previousTenant.Id != tenantInfo.Id)
        {
            _logger.LogWarning(
                "TenantContext: Overwriting an existing tenant context. Previous Tenant ID: '{PreviousTenantId}', New Tenant ID: '{NewTenantId}'. This might be expected during re-evaluation or indicate an issue.",
                previousTenant.Id,
                tenantInfo.Id);
        }
        else if (previousTenant != null && tenantInfo == null)
        {
            _logger.LogInformation(
                "TenantContext: Clearing previously set tenant context. Previous Tenant ID: '{PreviousTenantId}'.",
                previousTenant.Id);
        }

        _currentTenantAsyncLocal.Value = tenantInfo;

        if (tenantInfo != null)
        {
            _logger.LogDebug(
                "TenantContext: Current tenant set. Tenant ID: '{TenantId}', Status: '{TenantStatus}'. IsActiveAndResolved: {IsActiveAndResolved}",
                tenantInfo.Id,
                tenantInfo.Status,
                IsTenantResolvedAndActive); // Log the derived status too

            if (tenantInfo.Status != TenantStatus.Active)
            {
                // This is informational. The consuming code or middleware should decide how to act on non-active tenants.
                _logger.LogInformation(
                    "TenantContext: Tenant '{TenantId}' is resolved but its status is '{TenantStatus}'. Access control should be based on this status.",
                    tenantInfo.Id,
                    tenantInfo.Status);
            }
        }
        else
        {
            _logger.LogDebug("TenantContext: Current tenant explicitly set to null.");
        }
    }
}
