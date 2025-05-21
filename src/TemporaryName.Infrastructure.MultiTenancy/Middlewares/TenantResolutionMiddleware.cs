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

public partial class TenantResolutionMiddleware
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
        _options = optionsAccessor.CurrentValue; 
        _tenantContext = tenantContext;
        _strategyProvider = strategyProvider;
        _storeProvider = storeProvider;
        _logger = logger;

        if (_options is null)
        {
            Error error = new("MultiTenancy.Middleware.OptionsNull", "MultiTenancyOptions resolved to null. Middleware cannot operate.");

            LogOptionsNull(_logger, error.Code, error.Description);
            throw new TenantConfigurationException(error.Description!, error);
        }
        if (_options.Enabled && (_options.ResolutionStrategies is null || _options.ResolutionStrategies.Count == 0) && string.IsNullOrWhiteSpace(_options.DefaultTenantIdentifier) && string.IsNullOrWhiteSpace(_options.HostHandling.MapToTenantIdentifier))
        {
            LogNoResolutionStrategiesConfigured(_logger);
        }
        if (_options.Store is null)
        {
            Error error = new("MultiTenancy.Middleware.StoreOptionsNull", "MultiTenancyOptions.Store is null. Middleware cannot determine how to fetch tenants.");

            LogStoreOptionsNull(_logger, error.Code, error.Description);
            throw new TenantConfigurationException(error.Description!, error);
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        if (!_options.Enabled)
        {
            LogMultiTenancyDisabled(_logger);
            await _next(context);
            return;
        }

        ITenantInfo? resolvedTenantInfo = null;
        string? tenantIdentifier = null;
        bool identifierFromStrategy = false;

        LogResolutionProcessStarted(_logger, context.Request.Path);

        try
        {
            if (_options.ResolutionStrategies is not null)
            {
                var orderedStrategyOptsCol = _options.ResolutionStrategies.Where(s => s.IsEnabled).OrderBy(s => s.Order);

                foreach (TenantResolutionStrategyOptions strategyOpt in orderedStrategyOptsCol)
                {
                    LogAttemptingStrategy(_logger, strategyOpt.Type, strategyOpt.Order);

                    ITenantIdentificationStrategy strategy = _strategyProvider.GetStrategy(strategyOpt);
                    tenantIdentifier = await strategy.IdentifyTenantAsync(context).ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(tenantIdentifier))
                    {
                        LogIdentifierFoundByStrategy(_logger, tenantIdentifier, strategyOpt.Type);
                        identifierFromStrategy = true;
                        break;
                    }
                    LogStrategyDidNotYieldIdentifier(_logger, strategyOpt.Type);
                }
            }

            if (string.IsNullOrWhiteSpace(tenantIdentifier) && !string.IsNullOrWhiteSpace(_options.HostHandling.MapToTenantIdentifier))
            {
                LogAttemptingHostMapping(_logger, _options.HostHandling.MapToTenantIdentifier);

                tenantIdentifier = _options.HostHandling.MapToTenantIdentifier;
                identifierFromStrategy = false;
            }

            if (string.IsNullOrWhiteSpace(tenantIdentifier) && !string.IsNullOrWhiteSpace(_options.DefaultTenantIdentifier))
            {
                LogUsingDefaultTenantIdentifier(_logger, _options.DefaultTenantIdentifier);

                tenantIdentifier = _options.DefaultTenantIdentifier;
                identifierFromStrategy = false; 
            }

            if (!string.IsNullOrWhiteSpace(tenantIdentifier))
            {
                ITenantStore tenantStore = _storeProvider.GetStore(_options.Store);
                LogFetchingTenantInfoFromStore(_logger, tenantIdentifier, _options.Store.Type);

                resolvedTenantInfo = await tenantStore.GetTenantByIdentifierAsync(tenantIdentifier).ConfigureAwait(false);

                if (resolvedTenantInfo is null)
                {
                    Error error = new("Tenant.NotFound", $"Tenant with identifier '{tenantIdentifier}' not found in the configured store ({_options.Store.Type}).");
                    LogTenantNotFoundInStore(_logger, tenantIdentifier, _options.Store.Type, error.Code, error.Description);

                    if (tenantIdentifier == _options.DefaultTenantIdentifier || tenantIdentifier == _options.HostHandling.MapToTenantIdentifier)
                    {
                        Error configError = new("Tenant.Misconfigured.DefaultOrMappedNotFound", $"The configured DefaultTenantIdentifier or HostHandling.MapToTenantIdentifier '{tenantIdentifier}' was not found in the store.");

                        LogMisconfiguredDefaultOrMappedNotFound(_logger, tenantIdentifier, error.Code, error.Description);
                        throw new TenantNotFoundException(tenantIdentifier, configError);
                    }
                    throw new TenantNotFoundException(tenantIdentifier, error);
                }

                LogTenantResolvedSuccessfully(_logger, resolvedTenantInfo.Id, tenantIdentifier, resolvedTenantInfo.Status);
                }
            else
            {
                if (_options.HostHandling.AllowUnresolvedRequests)
                {
                    LogNoIdentifierAllowUnresolved(_logger);
                }
                else if (_options.ThrowIfTenantMissing)
                {
                    Error error = new("Tenant.ResolutionFailed.NotIdentified", "Tenant could not be identified from the request, and no default or host mapping was applicable. Tenant identification is required.");

                    LogNoIdentifierResolutionFailedRequired(_logger, context.Request.Path, error.Code, error.Description);
                    throw new TenantResolutionException(error);
                }
                else
                {
                    LogNoIdentifierProceedNullContext(_logger);
                }
            }

            if (resolvedTenantInfo is not null)
            {
                if (resolvedTenantInfo.Status != TenantStatus.Active)
                {
                    LogResolvedTenantNotActive(_logger, resolvedTenantInfo.Id, resolvedTenantInfo.Status);

                    //TODO
                    // Here, you might have more granular options, e.g., _options.ThrowIfTenantNotActive
                    // For now, let's assume if ThrowIfTenantMissing is true, non-active also means throw,
                    // unless specific exceptions are more appropriate.
                    // This is a critical policy decision. For FAANG-level, explicit failure for non-Active is often default.
                    //ACTION REQUIRED TODO

                    if (_options.ThrowIfTenantMissing)
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
                                throw new TenantDeactivatedException(resolvedTenantInfo.Id, new Error("Tenant.Status.Archived", $"Access denied: Tenant '{resolvedTenantInfo.Id}' is archived."));
                            case TenantStatus.Unknown:
                            default:
                                throw new TenantNotActiveException(resolvedTenantInfo.Id, resolvedTenantInfo.Status.ToString(), new Error("Tenant.Status.NotActive", $"Access denied: Tenant '{resolvedTenantInfo.Id}' is not in an active state (Status: {resolvedTenantInfo.Status})."));
                        }
                    }
                }

                resolvedTenantInfo = ApplyDefaultSettings(resolvedTenantInfo);
            }

            _tenantContext.SetCurrentTenant(resolvedTenantInfo);

            if (resolvedTenantInfo is not null)
            {
                LogResolutionCompleteTenantResolved(_logger, resolvedTenantInfo.Id, resolvedTenantInfo.Status);
            }
            else
            {
                LogResolutionCompleteNoTenant(_logger);
            }

        }
        catch (TenantResolutionException ex)
        {
            LogTenantResolutionExceptionCaught(_logger, ex.Message, ex);
            _tenantContext.SetCurrentTenant(null); 
            throw;
        }
        catch (TenantConfigurationException ex)
        {
            LogTenantConfigurationExceptionCaught(_logger, ex.Message, ex);
            _tenantContext.SetCurrentTenant(null);
            throw; 
        }
        catch (TenantStoreException ex)
        {
            LogTenantStoreExceptionCaught(_logger, ex.Message, ex);
            _tenantContext.SetCurrentTenant(null);
            throw;
        }
        catch (TenantCacheException ex)
        {
            LogTenantCacheExceptionCaught(_logger, ex.Message, ex);
            _tenantContext.SetCurrentTenant(null);
            // Policy: if cache fails, do we try to proceed without cache or fail request?
            // Current CachingTenantStoreDecorator throws if cache fails, so this will be caught.
            // If it were to fallback, this catch might not be hit for the original store error.
            throw;
        }
        catch (Exception ex) 
        {
            Error error = new("Tenant.Middleware.UnexpectedError", "An unexpected error occurred during tenant resolution.");
            LogMiddlewareUnexpectedError(_logger, context.Request.Path, error.Code, error.Description, ex);
            _tenantContext.SetCurrentTenant(null);

            throw new VagueMultiTenancyException(error.Description!, error, ex);
        }

        await _next(context);
    }

    private ITenantInfo? ApplyDefaultSettings(ITenantInfo tenantInfo)
    {
        if (_options.DefaultSettings is null) return tenantInfo;

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
            LogApplyingDefaultSettings(_logger, tenantInfo.Id,
                tenantInfo.PreferredLocale, tenantInfo.TimeZoneId, tenantInfo.DataRegion, tenantInfo.SubscriptionTier, tenantInfo.DataIsolationMode,
                _options.DefaultSettings.PreferredLocale, _options.DefaultSettings.TimeZoneId, _options.DefaultSettings.DataRegion, _options.DefaultSettings.SubscriptionTier, _options.DefaultSettings.DataIsolationMode);

            return new TenantInfo(
                tenantInfo.Id,
                tenantInfo.Name,
                tenantInfo.ConnectionStringName,
                tenantInfo.Status,
                tenantInfo.Domain,
                newSubscriptionTier ?? tenantInfo.SubscriptionTier,
                tenantInfo.BrandingName,
                tenantInfo.LogoUrl,
                newDataIsolationMode ?? tenantInfo.DataIsolationMode,
                tenantInfo.EnabledFeatures.ToList(), 
                tenantInfo.CustomProperties.ToDictionary(kv => kv.Key, kv => kv.Value), 
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

