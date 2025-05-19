using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;
using TemporaryName.Infrastructure.MultiTenancy.Exceptions;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Factories;

public partial class TenantOperationScopeFactory : ITenantOperationScopeFactory
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantStore _tenantStore;
    private readonly TenantDataOptions _tenantDataOptions;
    private readonly ILogger<TenantOperationScopeFactory> _logger;

    public TenantOperationScopeFactory(
        ITenantContext tenantContext,
        ITenantStore tenantStore,
        IOptionsMonitor<TenantDataOptions> tenantDataOptionsAccessor,
        ILogger<TenantOperationScopeFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(tenantContext, nameof(tenantContext));
        ArgumentNullException.ThrowIfNull(tenantStore, nameof(tenantStore));
        ArgumentNullException.ThrowIfNull(tenantDataOptionsAccessor, nameof(tenantDataOptionsAccessor));
        ArgumentNullException.ThrowIfNull(tenantDataOptionsAccessor.CurrentValue, nameof(tenantDataOptionsAccessor.CurrentValue));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _tenantContext = tenantContext;
        _tenantStore = tenantStore;
        _tenantDataOptions = tenantDataOptionsAccessor.CurrentValue;
        _logger = logger;
    }

    public async Task<ITenantOperationScope> CreateScopeAsync(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));
        LogCreatingTenantOperation(_logger, tenantId);

        ITenantInfo? tenantInfo = await _tenantStore.GetTenantByIdentifierAsync(tenantId).ConfigureAwait(false);
        if (tenantInfo == null)
        {
            Error error = new("Tenant.Scope.NotFoundOnCreate", $"Tenant with ID '{tenantId}' not found when trying to create an operation scope.");

            LogNotFoundOnCreate(_logger, tenantId);

            throw new TenantNotFoundException(tenantId, error);
        }
        return CreateScopeInternal(tenantInfo, $"Scope for TenantId: {tenantId} (fetched)");
    }

    public ITenantOperationScope CreateScope(ITenantInfo tenantInfo)
    {
        ArgumentNullException.ThrowIfNull(tenantInfo, nameof(tenantInfo));

        LogCreatingTenantOperation(_logger, tenantInfo.Id, $"provided {nameof(ITenantInfo)}. ");
        
        return CreateScopeInternal(tenantInfo, $"Scope for provided TenantInfo (ID: {tenantInfo.Id})");
    }

    private ITenantOperationScope CreateScopeInternal(ITenantInfo tenantInfoToSet, string scopeDescription)
    {
        if (tenantInfoToSet.Status != TenantStatus.Active)
        {
            string logMessage = $"Tenant '{tenantInfoToSet.Id}' is not active (Status: {tenantInfoToSet.Status}) during scope creation.";
            if (!_tenantDataOptions.AllowScopeCreationForNonActiveTenants)
            {
                Error error = Error.Forbidden(
                    "Tenant.Scope.NotActiveDisallowed",
                    $"{logMessage} Scope creation for non-active tenants is disallowed by configuration.",
                    new Dictionary<string, object?> {
                            { "TenantId", tenantInfoToSet.Id },
                            { "TenantStatus", tenantInfoToSet.Status.ToString() }
                    }
                );
                LogTenantNotActiveCreationDisallowed(_logger, error.Code, error.Description, error.Metadata);

                throw tenantInfoToSet.Status switch
                {
                    TenantStatus.Suspended => new TenantSuspendedException(tenantInfoToSet.Id, error),
                    TenantStatus.Deactivated => new TenantDeactivatedException(tenantInfoToSet.Id, error),
                    _ => new TenantNotActiveException(tenantInfoToSet.Id, tenantInfoToSet.Status.ToString(), error)
                };
            }

            LogTenantNotActive(_logger, logMessage, nameof(_tenantDataOptions.AllowScopeCreationForNonActiveTenants));
        }

        ITenantInfo? previousTenantInfo = _tenantContext.CurrentTenant;
        _tenantContext.SetCurrentTenant(tenantInfoToSet);

        LogTenantContextSwitched(_logger, previousTenantInfo?.Id, tenantInfoToSet.Id, scopeDescription);

        return new TenantOperationScope(tenantInfoToSet, previousTenantInfo, _tenantContext, _logger);
    }
}
