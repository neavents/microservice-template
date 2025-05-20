using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Stores;

/// <summary>
/// An <see cref="ITenantStore"/> implementation that stores tenant information in memory.
/// This store can be populated programmatically and is useful for testing or dynamic scenarios
/// where tenants are not solely defined by static configuration.
/// It differs from ConfigurationTenantStore which is read-only from IOptions.
/// This store could allow adding/updating tenants at runtime if such methods were exposed.
/// </summary>
public partial class InMemoryTenantStore : ITenantStore
{
    private readonly ConcurrentDictionary<string, ITenantInfo> _tenantsByIdentifier;
    private readonly ILogger<InMemoryTenantStore> _logger;

    public InMemoryTenantStore(ILogger<InMemoryTenantStore> logger, IEnumerable<ITenantInfo>? initialTenants = null)
    {
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _logger = logger;

        _tenantsByIdentifier = new ConcurrentDictionary<string, ITenantInfo>(StringComparer.OrdinalIgnoreCase);

        if (initialTenants != null)
        {
            foreach (ITenantInfo tenant in initialTenants)
            {
                if (tenant == null || string.IsNullOrWhiteSpace(tenant.Id))
                {
                    LogSkippingNullTenantOnInit(_logger);
                    continue;
                }

                if (!_tenantsByIdentifier.TryAdd(tenant.Id, tenant))
                {
                    LogDuplicateTenantIdOnInit(_logger, tenant.Id);
                }
            }
        }
        LogInitializationSuccess(_logger, _tenantsByIdentifier.Count);
    }

    public Task<ITenantInfo?> GetTenantByIdentifierAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            LogGetTenantCalledWithNullOrEmptyId(_logger);
            return Task.FromResult<ITenantInfo?>(null);
        }

        if (_tenantsByIdentifier.TryGetValue(id, out ITenantInfo? tenantInfo))
        {
            LogTenantFoundByIdentifier(_logger, id, tenantInfo.Id, tenantInfo.Status);
            return Task.FromResult<ITenantInfo?>(tenantInfo);
        }

        LogTenantNotFoundByIdentifier(_logger, id);
        return Task.FromResult<ITenantInfo?>(null);
    }

    /// <summary>
    /// Adds or updates a tenant in the store. Useful for programmatic population or testing.
    /// The 'identifierForLookup' is the key that will be used by GetTenantByIdentifierAsync.
    /// </summary>
    public bool TryAddOrUpdateTenant(string identifierForLookup, ITenantInfo tenantInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifierForLookup, nameof(identifierForLookup));
        ArgumentNullException.ThrowIfNull(tenantInfo, nameof(tenantInfo));
        // TenantInfo constructor already validates tenantInfo.Id

        _tenantsByIdentifier.AddOrUpdate(identifierForLookup, tenantInfo, (key, existingVal) => tenantInfo);

        LogTenantAddedOrUpdated(_logger, identifierForLookup, tenantInfo.Id);
        return true;
    }

    /// <summary>
    /// Removes a tenant from the store using its lookup identifier.
    /// </summary>
    public bool TryRemoveTenant(string identifierForLookup)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifierForLookup, nameof(identifierForLookup));
        if (_tenantsByIdentifier.TryRemove(identifierForLookup, out ITenantInfo? removedTenant))
        {
            LogTenantRemoved(_logger, identifierForLookup, removedTenant.Id);
            return true;
        }
        LogRemoveTenantNotFound(_logger, identifierForLookup);
        return false;
    }
}