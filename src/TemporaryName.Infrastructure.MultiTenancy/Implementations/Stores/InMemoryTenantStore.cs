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
                    _logger.LogWarning("Skipping null tenant or tenant with null/empty ID during InMemoryTenantStore initialization.");
                    continue;
                }

                if (!_tenantsByIdentifier.TryAdd(tenant.Id, tenant))
                {
                    _logger.LogWarning("Duplicate tenant ID '{TenantId}' encountered during InMemoryTenantStore initialization. The first entry was kept.", tenant.Id);
                }
            }
        }
        _logger.LogInformation("InMemoryTenantStore initialized with {TenantCount} tenants.", _tenantsByIdentifier.Count);
    }

    public Task<ITenantInfo?> GetTenantByIdentifierAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogDebug("InMemoryTenantStore.GetTenantByIdentifierAsync called with null or empty identifier.");
            return Task.FromResult<ITenantInfo?>(null);
        }

        if (_tenantsByIdentifier.TryGetValue(id, out ITenantInfo? tenantInfo))
        {
            _logger.LogDebug("Tenant found in InMemoryTenantStore for identifier '{Identifier}'. Tenant ID: '{TenantId}', Status: '{TenantStatus}'.", id, tenantInfo.Id, tenantInfo.Status);
            return Task.FromResult(tenantInfo);
        }

        _logger.LogDebug("No tenant found in InMemoryTenantStore for identifier '{Identifier}'.", id);
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
        _logger.LogInformation("Tenant with lookup identifier '{IdentifierForLookup}' (Tenant ID: '{TenantId}') was added/updated in InMemoryTenantStore.", identifierForLookup, tenantInfo.Id);
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
            _logger.LogInformation("Tenant with lookup identifier '{IdentifierForLookup}' (Tenant ID: '{TenantId}') was removed from InMemoryTenantStore.", identifierForLookup, removedTenant?.Id);
            return true;
        }
        _logger.LogWarning("Attempted to remove tenant with lookup identifier '{IdentifierForLookup}', but it was not found in InMemoryTenantStore.", identifierForLookup);
        return false;
    }
}