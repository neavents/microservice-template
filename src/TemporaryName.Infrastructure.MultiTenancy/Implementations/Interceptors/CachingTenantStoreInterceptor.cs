using System;
using System.Reflection;
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
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger<CachingTenantStoreInterceptor> _logger;

    public CachingTenantStoreInterceptor(
        IMemoryCache memoryCache,
        IOptions<MultiTenancyOptions> multiTenancyOptionsAccessor,
        ILogger<CachingTenantStoreInterceptor> logger)
    {
        ArgumentNullException.ThrowIfNull(memoryCache, nameof(memoryCache));
        ArgumentNullException.ThrowIfNull(multiTenancyOptionsAccessor, nameof(multiTenancyOptionsAccessor));
        ArgumentNullException.ThrowIfNull(multiTenancyOptionsAccessor.Value, nameof(multiTenancyOptionsAccessor.Value));
        ArgumentNullException.ThrowIfNull(multiTenancyOptionsAccessor.Value.Store, nameof(multiTenancyOptionsAccessor.Value.Store));
        ArgumentNullException.ThrowIfNull(multiTenancyOptionsAccessor.Value.Store.Cache, nameof(multiTenancyOptionsAccessor.Value.Store.Cache));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _memoryCache = memoryCache;
        _cacheOptions = multiTenancyOptionsAccessor.Value.Store.Cache;
        _logger = logger;

        if (!_cacheOptions.Enabled)
        {
            LogInterceptorInstantiatedButCacheIsDisabled(_logger);
        }
        if (_cacheOptions.AbsoluteExpirationSeconds <= 0 && _cacheOptions.SlidingExpirationSeconds <= 0 && _cacheOptions.Enabled)
        {
            LogStrangeCacheExpirationAndSliding(_logger);
        }
    }

    public void Intercept(IInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        if (!_cacheOptions.Enabled)
        {
            LogProceedingWithoutCaching(_logger, invocation.Method.Name);
            invocation.Proceed();
            return;
        }

        if (invocation.Method.Name == nameof(ITenantStore.GetTenantByIdentifierAsync) &&
            invocation.Arguments.Length == 1 &&
            invocation.Arguments[0] is string identifier &&
            !string.IsNullOrWhiteSpace(identifier))
        {
            string cacheKey = $"TenantInfoByIdentifier_{identifier}";
            LogIntercepting(_logger, invocation.Method.Name, identifier, cacheKey);

            if (_memoryCache.TryGetValue(cacheKey, out object? cachedResult))
            {
                LogTenantCacheHit(_logger, identifier, invocation.Method.Name, cacheKey);
                invocation.ReturnValue = cachedResult;
                return;
            }

            LogTenantCacheMiss(_logger, identifier, invocation.Method.Name, cacheKey);
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
                        LogUnderlyingCallFailed(_logger, invocation.Method.Name, identifier, cacheKey, null);
                        return;
                    }
                    if (t.IsCanceled)
                    {
                        LogUnderlyingCallCancelled(_logger, invocation.Method.Name, identifier, cacheKey);
                        return;
                    }

                    object? resultToCache = null;
                    if (invocation.Method.ReturnType.IsGenericType &&
                        invocation.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        try
                        {
                            PropertyInfo? resultProperty = t.GetType().GetProperty("Result");
                            if (resultProperty != null)
                            {
                                resultToCache = resultProperty.GetValue(t);
                            }
                            else
                            {
                                LogCompletedTaskDoesNotHaveResultProperty(_logger, invocation.Method.Name, identifier, t.GetType());
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogCanNotAccessResultOfCompletedTaskForCaching(_logger, invocation.Method.Name, identifier, ex);
                            return;
                        }
                        ;
                    }
                    else
                    {
                        LogCanNotExtractResultForCaching(_logger, invocation.Method.Name);
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
                        LogCachingTheResult(_logger, invocation.Method.Name, identifier, cacheKey, resultToCache is null ? "null" : "not null");
                        _memoryCache.Set(cacheKey, invocation.ReturnValue, entryOptions);
                    }
                    else
                    {
                        LogFetchedButNotCachedBecauseExpiration(_logger, invocation.Method.Name, identifier);
                    }

                },  CancellationToken.None,
                    TaskContinuationOptions.DenyChildAttach,
                    TaskScheduler.Default);
            }
            else
            {
                LogInterceptedButNotReturnedTask(_logger, invocation.Method.Name);
            }
        }
        else
        {
            LogCriteriaOrArgumentMisconfiguration(_logger, invocation.Method.Name);
            invocation.Proceed();
        }
    }
}