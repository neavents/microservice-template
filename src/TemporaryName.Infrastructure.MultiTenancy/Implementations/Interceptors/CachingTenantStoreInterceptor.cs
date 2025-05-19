using System;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Interceptors;

/// <summary>
/// Autofac interceptor for caching results of ITenantStore methods.
/// Specifically targets GetTenantByIdentifierAsync.
/// </summary>
public partial class CachingTenantStoreInterceptor : IInterceptor
{
    private readonly IMemoryCache _memoryCache;
    private readonly CacheOptions _cacheOptions; // Directly injected CacheOptions
    private readonly ILogger<CachingTenantStoreInterceptor> _logger;

    public CachingTenantStoreInterceptor(
        IMemoryCache memoryCache,
        IOptions<MultiTenancyOptions> multiTenancyOptionsAccessor, // To get CacheOptions
        ILogger<CachingTenantStoreInterceptor> logger)
    {
        ArgumentNullException.ThrowIfNull(memoryCache, nameof(memoryCache));
        ArgumentNullException.ThrowIfNull(multiTenancyOptionsAccessor, nameof(multiTenancyOptionsAccessor));
        ArgumentNullException.ThrowIfNull(multiTenancyOptionsAccessor.Value, nameof(multiTenancyOptionsAccessor.Value));
        ArgumentNullException.ThrowIfNull(multiTenancyOptionsAccessor.Value.Store, nameof(multiTenancyOptionsAccessor.Value.Store));
        ArgumentNullException.ThrowIfNull(multiTenancyOptionsAccessor.Value.Store.Cache, nameof(multiTenancyOptionsAccessor.Value.Store.Cache));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _memoryCache = memoryCache;
        _cacheOptions = multiTenancyOptionsAccessor.Value.Store.Cache; // Extract CacheOptions
        _logger = logger;

        if (!_cacheOptions.Enabled)
        {
            _logger.LogWarning("CachingTenantStoreInterceptor instantiated, but CacheOptions.Enabled is false. Interception will likely bypass caching.");
        }
        if (_cacheOptions.AbsoluteExpirationSeconds <= 0 && _cacheOptions.SlidingExpirationSeconds <= 0 && _cacheOptions.Enabled)
        {
            _logger.LogWarning("CachingTenantStoreInterceptor: Cache is enabled but both AbsoluteExpirationSeconds and SlidingExpirationSeconds are non-positive. Items might not be cached effectively.");
        }
    }

    public void Intercept(IInvocation invocation)
    {
        if (!_cacheOptions.Enabled)
        {
            _logger.LogTrace("Caching is disabled, proceeding with invocation for {MethodName} without caching.", invocation.Method.Name);
            invocation.Proceed();
            return;
        }

        if (invocation.Method.Name == nameof(ITenantStore.GetTenantByIdentifierAsync) &&
            invocation.Arguments.Length == 1 &&
            invocation.Arguments[0] is string identifier &&
            !string.IsNullOrWhiteSpace(identifier))
        {
            string cacheKey = $"TenantInfoByIdentifier_{identifier}";
            _logger.LogTrace("Intercepting {MethodName} for identifier '{Identifier}'. Cache key: '{CacheKey}'.", invocation.Method.Name, identifier, cacheKey);

            if (_memoryCache.TryGetValue(cacheKey, out object? cachedResult))
            {
                _logger.LogDebug("Tenant '{Identifier}' found in cache for method {MethodName}. Cache key: '{CacheKey}'.", identifier, invocation.Method.Name, cacheKey);
                invocation.ReturnValue = cachedResult;
                return;
            }

            _logger.LogDebug("Tenant '{Identifier}' not found in cache for method {MethodName}. Proceeding with actual call. Cache key: '{CacheKey}'.", identifier, invocation.Method.Name, cacheKey);
            invocation.Proceed();

            // Post-processing for async methods:
            // The ReturnValue will be a Task<ITenantInfo?>. We need to await it (or attach a continuation)
            // to get the actual result for caching.
            if (invocation.ReturnValue is Task task)
            {
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _logger.LogWarning(t.Exception, "Underlying call for {MethodName} (identifier: {Identifier}, cache key: {CacheKey}) failed. Not caching faulted task.", invocation.Method.Name, identifier, cacheKey);
                        // Optionally, cache the exception or a marker for a short period to prevent hammering a failing downstream service.
                        // For now, we don't cache failures.
                        return;
                    }
                    if (t.IsCanceled)
                    {
                        _logger.LogWarning("Underlying call for {MethodName} (identifier: {Identifier}, cache key: {CacheKey}) was canceled. Not caching canceled task.", invocation.Method.Name, identifier, cacheKey);
                        return;
                    }

                    // Get the result from the task to cache it.
                    // This requires knowing the Task's result type.
                    object? resultToCache = null;
                    if (invocation.Method.ReturnType.IsGenericType &&
                        invocation.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        try
                        {
                            resultToCache = ((dynamic)t).Result; // Access Task<T>.Result
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error accessing result of completed task for caching. Method: {MethodName}, Identifier: {Identifier}", invocation.Method.Name, identifier);
                            return; // Cannot cache if result cannot be accessed
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Return type of {MethodName} is Task but not Task<T>. Cannot extract result for caching.", invocation.Method.Name);
                        return; // Should not happen for GetTenantByIdentifierAsync
                    }


                    MemoryCacheEntryOptions entryOptions = new();
                    if (_cacheOptions.AbsoluteExpirationSeconds > 0)
                    {
                        entryOptions.SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheOptions.AbsoluteExpirationSeconds));
                    }
                    if (_cacheOptions.SlidingExpirationSeconds > 0)
                    {
                        entryOptions.SetSlidingExpiration(TimeSpan.FromSeconds(_cacheOptions.SlidingExpirationSeconds));
                    }

                    if (entryOptions.AbsoluteExpiration.HasValue || entryOptions.SlidingExpiration.HasValue)
                    {
                        _logger.LogDebug("Caching result for {MethodName} (identifier: {Identifier}) with key '{CacheKey}'. Result: {ResultIsNull}",
                            invocation.Method.Name, identifier, cacheKey, resultToCache == null ? "null" : "not null");
                        _memoryCache.Set(cacheKey, invocation.ReturnValue, entryOptions);
                    }
                    else
                    {
                        _logger.LogWarning("Result for {MethodName} (identifier: {Identifier}) fetched but not cached as no valid expiration was configured.", invocation.Method.Name, identifier);
                    }

                }, TaskContinuationOptions.ExecuteSynchronously);
            }
            else
            {
                _logger.LogWarning("Method {MethodName} was intercepted but did not return a Task. Caching for non-Task return types is not implemented in this interceptor.", invocation.Method.Name);
            }
        }
        else
        {
            _logger.LogTrace("Method {MethodName} does not match caching criteria or arguments are invalid. Proceeding without caching.", invocation.Method.Name);
            invocation.Proceed();
        }
    }
}