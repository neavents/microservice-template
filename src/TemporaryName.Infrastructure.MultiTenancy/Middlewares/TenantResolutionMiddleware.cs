using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;
using TemporaryName.Infrastructure.MultiTenancy.Exceptions;
using TemporaryName.Infrastructure.MultiTenancy.Implementations;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy.Middlewares;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;
    private readonly MultiTenancyOptions _options;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantStrategyProvider _strategyProvider;
    private readonly ITenantStoreProvider _storeProvider;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        IOptionsMonitor<MultiTenancyOptions> optionsAccessor,
        ITenantContext tenantContext,
        ITenantStrategyProvider strategyProvider,
        ITenantStoreProvider storeProvider,
        ILogger<TenantResolutionMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next, nameof(next));
        ArgumentNullException.ThrowIfNull(optionsAccessor, nameof(optionsAccessor));
        ArgumentNullException.ThrowIfNull(tenantContext, nameof(tenantContext));
        ArgumentNullException.ThrowIfNull(strategyProvider, nameof(strategyProvider));
        ArgumentNullException.ThrowIfNull(storeProvider, nameof(storeProvider));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _next = next;
        _options = optionsAccessor.CurrentValue; // Options are read once at middleware construction.
        _tenantContext = tenantContext;
        _strategyProvider = strategyProvider;
        _storeProvider = storeProvider;
        _logger = logger;

        // Validate critical options at startup
        if (_options == null)
        {
            Error error = new("MultiTenancy.Middleware.OptionsNull", "MultiTenancyOptions resolved to null. Middleware cannot operate.");
            _logger.LogCritical(error.Description);
            throw new TenantConfigurationException(error.Description, error); // Fail fast
        }
        if (_options.Enabled && (_options.ResolutionStrategies == null || !_options.ResolutionStrategies.Any()) && string.IsNullOrWhiteSpace(_options.DefaultTenantIdentifier) && string.IsNullOrWhiteSpace(_options.HostHandling.MapToTenantIdentifier))
        {
            _logger.LogWarning("MultiTenancy is enabled, but no resolution strategies are configured, no default tenant identifier is set, and no host mapping is defined. Tenant resolution will likely fail for all requests.");
        }
        if (_options.Store == null)
        {
            Error error = new("MultiTenancy.Middleware.StoreOptionsNull", "MultiTenancyOptions.Store is null. Middleware cannot determine how to fetch tenants.");
            _logger.LogCritical(error.Description);
            throw new TenantConfigurationException(error.Description, error);
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        if (!_options.Enabled)
        {
            _logger.LogDebug("MultiTenancy is disabled. Skipping tenant resolution.");
            await _next(context);
            return;
        }

        ITenantInfo? resolvedTenantInfo = null;
        string? tenantIdentifier = null;
        bool identifierFromStrategy = false;

        _logger.LogDebug("Tenant resolution process started for request path: {Path}", context.Request.Path);

        try
        {
            // 1. Attempt to identify tenant using configured strategies
            if (_options.ResolutionStrategies != null)
            {
                // Sort strategies by Order if not already sorted (though typically configured in order)
                foreach (TenantResolutionStrategyOptions strategyOpt in _options.ResolutionStrategies.Where(s => s.IsEnabled).OrderBy(s => s.Order))
                {
                    _logger.LogDebug("Attempting strategy: {StrategyType} (Order: {Order})", strategyOpt.Type, strategyOpt.Order);
                    ITenantIdentificationStrategy strategy = _strategyProvider.GetStrategy(strategyOpt);
                    tenantIdentifier = await strategy.IdentifyTenantAsync(context);

                    if (!string.IsNullOrWhiteSpace(tenantIdentifier))
                    {
                        _logger.LogInformation("Tenant identifier '{Identifier}' found using strategy {StrategyType}.", tenantIdentifier, strategyOpt.Type);
                        identifierFromStrategy = true;
                        break;
                    }
                    _logger.LogDebug("Strategy {StrategyType} did not yield an identifier.", strategyOpt.Type);
                }
            }

            // 2. If no identifier from strategies, check HostHandlingOptions for mapping
            if (string.IsNullOrWhiteSpace(tenantIdentifier) && !string.IsNullOrWhiteSpace(_options.HostHandling.MapToTenantIdentifier))
            {
                _logger.LogInformation("No tenant identified by strategies. Attempting to map host request to tenant identifier: '{MapToTenantIdentifier}'.", _options.HostHandling.MapToTenantIdentifier);
                tenantIdentifier = _options.HostHandling.MapToTenantIdentifier;
                identifierFromStrategy = false; // This is a mapped identifier, not directly from a strategy for the current request context
            }


            // 3. If still no identifier, try DefaultTenantIdentifier
            if (string.IsNullOrWhiteSpace(tenantIdentifier) && !string.IsNullOrWhiteSpace(_options.DefaultTenantIdentifier))
            {
                _logger.LogInformation("No tenant identifier from strategies or host mapping. Using DefaultTenantIdentifier: '{DefaultTenantIdentifier}'.", _options.DefaultTenantIdentifier);
                tenantIdentifier = _options.DefaultTenantIdentifier;
                identifierFromStrategy = false; // This is a default
            }

            // 4. If an identifier was found (from any source), try to fetch ITenantInfo
            if (!string.IsNullOrWhiteSpace(tenantIdentifier))
            {
                ITenantStore tenantStore = _storeProvider.GetStore(_options.Store);
                _logger.LogDebug("Attempting to fetch tenant info for identifier '{Identifier}' from store type {StoreType}.", tenantIdentifier, _options.Store.Type);
                resolvedTenantInfo = await tenantStore.GetTenantByIdentifierAsync(tenantIdentifier);

                if (resolvedTenantInfo == null)
                {
                    Error error = new("Tenant.NotFound", $"Tenant with identifier '{tenantIdentifier}' not found in the configured store ({_options.Store.Type}).");
                    _logger.LogWarning(error.Description);
                    // If the DefaultTenantIdentifier or MappedHostIdentifier was used and not found, it's a critical misconfiguration.
                    if (tenantIdentifier == _options.DefaultTenantIdentifier || tenantIdentifier == _options.HostHandling.MapToTenantIdentifier)
                    {
                        Error configError = new("Tenant.Misconfigured.DefaultOrMappedNotFound", $"The configured DefaultTenantIdentifier or HostHandling.MapToTenantIdentifier '{tenantIdentifier}' was not found in the store.");
                        _logger.LogCritical(configError.Description);
                        throw new TenantNotFoundException(tenantIdentifier, configError); // More specific
                    }
                    throw new TenantNotFoundException(tenantIdentifier, error);
                }
                _logger.LogInformation("Tenant '{TenantId}' (Identifier: '{Identifier}') resolved successfully from store. Status: {TenantStatus}", resolvedTenantInfo.Id, tenantIdentifier, resolvedTenantInfo.Status);
            }
            else
            {
                // No identifier found from strategies, no default, no host mapping.
                // Check if unresolved requests are allowed for host handling.
                if (_options.HostHandling.AllowUnresolvedRequests)
                {
                    _logger.LogInformation("No tenant identifier resolved, and HostHandling.AllowUnresolvedRequests is true. Proceeding with a null tenant context.");
                    // resolvedTenantInfo remains null
                }
                else if (_options.ThrowIfTenantMissing)
                {
                    Error error = new("Tenant.ResolutionFailed.NotIdentified", "Tenant could not be identified from the request, and no default or host mapping was applicable. Tenant identification is required.");
                    _logger.LogWarning(error.Description + " Request Path: {Path}", context.Request.Path);
                    throw new TenantResolutionException(error); // General resolution failure
                }
                else
                {
                    _logger.LogInformation("No tenant identifier resolved, and ThrowIfTenantMissing is false. Proceeding with a null tenant context.");
                    // resolvedTenantInfo remains null
                }
            }


            // 5. Validate tenant status if a tenant was resolved
            if (resolvedTenantInfo != null)
            {
                if (resolvedTenantInfo.Status != TenantStatus.Active)
                {
                    _logger.LogWarning("Resolved tenant '{TenantId}' is not active. Status: {TenantStatus}.", resolvedTenantInfo.Id, resolvedTenantInfo.Status);
                    // Here, you might have more granular options, e.g., _options.ThrowIfTenantNotActive
                    // For now, let's assume if ThrowIfTenantMissing is true, non-active also means throw,
                    // unless specific exceptions are more appropriate.
                    // This is a critical policy decision. For FAANG-level, explicit failure for non-Active is often default.
                    if (_options.ThrowIfTenantMissing) // Re-using this flag, could be more specific.
                    {
                        switch (resolvedTenantInfo.Status)
                        {
                            case TenantStatus.Suspended:
                                throw new TenantSuspendedException(resolvedTenantInfo.Id, new Error("Tenant.Status.Suspended", $"Access denied: Tenant '{resolvedTenantInfo.Id}' is suspended."), null); // Add suspension end date if available
                            case TenantStatus.Deactivated:
                                throw new TenantDeactivatedException(resolvedTenantInfo.Id, new Error("Tenant.Status.Deactivated", $"Access denied: Tenant '{resolvedTenantInfo.Id}' is deactivated."));
                            case TenantStatus.Provisioning:
                                throw new TenantProvisioningIncompleteException(resolvedTenantInfo.Id, new Error("Tenant.Status.Provisioning", $"Access temporarily unavailable: Tenant '{resolvedTenantInfo.Id}' is still provisioning."));
                            case TenantStatus.Archived:
                                throw new TenantDeactivatedException(resolvedTenantInfo.Id, new Error("Tenant.Status.Archived", $"Access denied: Tenant '{resolvedTenantInfo.Id}' is archived.")); // Similar to deactivated
                            case TenantStatus.Unknown:
                            default:
                                throw new TenantNotActiveException(resolvedTenantInfo.Id, resolvedTenantInfo.Status.ToString(), new Error("Tenant.Status.NotActive", $"Access denied: Tenant '{resolvedTenantInfo.Id}' is not in an active state (Status: {resolvedTenantInfo.Status})."));
                        }
                    }
                    // If not throwing, the non-active tenant will be set in the context, and consumers must check.
                }

                // 6. Apply DefaultTenantSettings (if any)
                resolvedTenantInfo = ApplyDefaultSettings(resolvedTenantInfo);
            }

            // 7. Set the resolved tenant (or null) in the context
            _tenantContext.SetCurrentTenant(resolvedTenantInfo);

            if (resolvedTenantInfo != null)
            {
                _logger.LogInformation("Tenant resolution complete. Current Tenant ID: '{TenantId}', Status: {TenantStatus}. Proceeding with request.", resolvedTenantInfo.Id, resolvedTenantInfo.Status);
            }
            else
            {
                _logger.LogInformation("Tenant resolution complete. No tenant resolved. Proceeding with request (null tenant context).");
            }

        }
        catch (TenantResolutionException ex) // Catch exceptions specifically from resolution logic or strategies
        {
            _logger.LogError(ex, "TenantResolutionException caught by middleware: {ErrorMessage}. Tenant resolution failed.", ex.ErrorDetails.Description);
            _tenantContext.SetCurrentTenant(null); // Ensure context is cleared on failure
                                                   // Depending on API vs UI, you might re-throw to a global handler or return an error response directly.
                                                   // For now, re-throw to be handled by ASP.NET Core's exception handling.
            throw;
        }
        catch (TenantConfigurationException ex)
        {
            _logger.LogCritical(ex, "TenantConfigurationException caught by middleware: {ErrorMessage}. This indicates a severe misconfiguration.", ex.ErrorDetails.Description);
            _tenantContext.SetCurrentTenant(null);
            throw; // Critical configuration errors should likely stop the request processing.
        }
        catch (TenantStoreException ex) // Covers StoreUnavailable, QueryFailed, Deserialization from stores
        {
            _logger.LogError(ex, "TenantStoreException caught by middleware: {ErrorMessage}. Failed to retrieve tenant from store.", ex.ErrorDetails.Description);
            _tenantContext.SetCurrentTenant(null);
            throw;
        }
        catch (TenantCacheException ex)
        {
            _logger.LogError(ex, "TenantCacheException caught by middleware: {ErrorMessage}. Error interacting with tenant cache.", ex.ErrorDetails.Description);
            _tenantContext.SetCurrentTenant(null);
            // Policy: if cache fails, do we try to proceed without cache or fail request?
            // Current CachingTenantStoreDecorator throws if cache fails, so this will be caught.
            // If it were to fallback, this catch might not be hit for the original store error.
            throw;
        }
        catch (Exception ex) // Catch-all for truly unexpected errors in the middleware
        {
            Error error = new("Tenant.Middleware.UnexpectedError", "An unexpected error occurred during tenant resolution.");
            _logger.LogCritical(ex, error.Description + " Request Path: {Path}", context.Request.Path);
            _tenantContext.SetCurrentTenant(null);

            throw new VagueMultiTenancyException(error.Description, error, ex);
        }

        await _next(context);
    }

    private ITenantInfo? ApplyDefaultSettings(ITenantInfo tenantInfo)
    {
        if (_options.DefaultSettings == null) return tenantInfo;

        // This is a bit verbose. In a real scenario, you might use reflection or a mapping library
        // if there are many properties, or create a new TenantInfo instance by copying and overriding.
        // For FAANG level, creating a new instance to ensure immutability of the original from store is better.

        bool settingsApplied = false;
        string? newPreferredLocale = tenantInfo.PreferredLocale ?? _options.DefaultSettings.PreferredLocale;
        string? newTimeZoneId = tenantInfo.TimeZoneId ?? _options.DefaultSettings.TimeZoneId;
        string? newDataRegion = tenantInfo.DataRegion ?? _options.DefaultSettings.DataRegion;
        string? newSubscriptionTier = tenantInfo.SubscriptionTier ?? _options.DefaultSettings.SubscriptionTier;
        TenantDataIsolationMode? newDataIsolationMode = tenantInfo.DataIsolationMode; // Enum, check if default is set
        if (_options.DefaultSettings.DataIsolationMode.HasValue && tenantInfo.DataIsolationMode == default(TenantDataIsolationMode)) // Assuming 'Unknown' or 0 is the default for the enum if not set
        {
            // This logic for enums is tricky. If TenantDataIsolationMode has a meaningful 0 value (like SharedDatabaseSharedSchema)
            // then this check needs to be more specific, e.g. if it was explicitly set to a "not set" marker.
            // For now, assuming if it's the default enum value and a default setting exists, apply it.
            // A better approach for enums might be to have them nullable in TenantInfo if "not set" is a valid state distinct from an actual enum value.
            // Or, the DTO from store should make this distinction clear.
            // Let's assume DataIsolationMode on tenantInfo is always a valid value from store or its own default.
            // We only override if the DefaultSetting is present and the tenant's current value is considered "unset" or default.
            // This part needs careful thought based on how "unset" is represented for enums.
            // For simplicity, let's assume if DefaultSettings.DataIsolationMode has a value, we use it if tenantInfo's is the enum default.
            // This is a simplification. A more robust way is to have nullable properties in DefaultTenantSettingsOptions.
            if (tenantInfo.DataIsolationMode == default(TenantDataIsolationMode) && _options.DefaultSettings.DataIsolationMode.HasValue)
            {
                newDataIsolationMode = _options.DefaultSettings.DataIsolationMode.Value;
            }
        }


        if (newPreferredLocale != tenantInfo.PreferredLocale ||
            newTimeZoneId != tenantInfo.TimeZoneId ||
            newDataRegion != tenantInfo.DataRegion ||
            newSubscriptionTier != tenantInfo.SubscriptionTier ||
            (newDataIsolationMode.HasValue && newDataIsolationMode.Value != tenantInfo.DataIsolationMode)
            )
        {
            settingsApplied = true;
        }

        if (settingsApplied)
        {
            _logger.LogDebug("Applying default settings to tenant '{TenantId}'. Original: Locale={OL}, TZ={OTZ}, Region={OR}, Tier={OST}, Isolation={ODI}. Defaults: Locale={DL}, TZ={DTZ}, Region={DR}, Tier={DST}, Isolation={DDI}",
                tenantInfo.Id,
                tenantInfo.PreferredLocale, tenantInfo.TimeZoneId, tenantInfo.DataRegion, tenantInfo.SubscriptionTier, tenantInfo.DataIsolationMode,
                _options.DefaultSettings.PreferredLocale, _options.DefaultSettings.TimeZoneId, _options.DefaultSettings.DataRegion, _options.DefaultSettings.SubscriptionTier, _options.DefaultSettings.DataIsolationMode);

            // Create a new TenantInfo instance with applied defaults to maintain immutability of the original
            return new TenantInfo(
                tenantInfo.Id,
                tenantInfo.Name,
                tenantInfo.ConnectionStringName,
                tenantInfo.Status,
                tenantInfo.Domain,
                newSubscriptionTier ?? tenantInfo.SubscriptionTier, // Prioritize new if set
                tenantInfo.BrandingName,
                tenantInfo.LogoUrl,
                newDataIsolationMode ?? tenantInfo.DataIsolationMode, // Prioritize new if set
                tenantInfo.EnabledFeatures.ToList(), // Pass as IEnumerable
                tenantInfo.CustomProperties.ToDictionary(kv => kv.Key, kv => kv.Value), // Pass as IDictionary
                newPreferredLocale ?? tenantInfo.PreferredLocale,
                newTimeZoneId ?? tenantInfo.TimeZoneId,
                newDataRegion ?? tenantInfo.DataRegion,
                tenantInfo.ParentTenantId,
                tenantInfo.CreatedAtUtc,
                tenantInfo.UpdatedAtUtc,
                tenantInfo.ConcurrencyStamp
            );
        }

        return tenantInfo;
    }
}

