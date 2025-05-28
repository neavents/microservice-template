using System;
using System.Text.Json;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.Caching.Abstractions;
using TemporaryName.Infrastructure.Caching.Abstractions.Settings;
using TemporaryName.Infrastructure.Caching.Memcached.Settings;

namespace TemporaryName.Infrastructure.Caching.Memcached.Implementations;

public partial class MemcachedCacheService : ICacheService
{
    private readonly IMemcachedClient _memcachedClient;
    private readonly MemcachedCacheOptions _options;
    private readonly ILogger<MemcachedCacheService> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    public MemcachedCacheService(
        IMemcachedClient memcachedClient,
        IOptions<MemcachedCacheOptions> options,
        ILogger<MemcachedCacheService> logger)
    {
        _memcachedClient = memcachedClient ?? throw new ArgumentNullException(nameof(memcachedClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        LogCacheGetAsync(_logger, key);

        try
        {
            IGetOperationResult<string> result = await _memcachedClient.GetAsync<string>(key).ConfigureAwait(false);
            if (!result.Success || !result.HasValue)
            {
                LogCacheGetMiss(_logger, key);
                return default;
            }

            string? stringValue = result.Value;
            if (stringValue is null)
            {
                LogCacheGetMiss(_logger, key);
                return default;
            }

            if (typeof(T) == typeof(string))
            {
                LogCacheGetHit(_logger, key, typeof(T).Name);
                return (T)(object)stringValue;
            }

            try
            {
                T? value = JsonSerializer.Deserialize<T>(stringValue, _serializerOptions);
                LogCacheGetHit(_logger, key, typeof(T).Name);
                return value;
            }
            catch (JsonException jsonEx)
            {
                LogCacheDeserializationError(_logger, key, jsonEx.Message, jsonEx);
                return default;
            }
        }
        catch (Exception ex)
        {
            LogCacheProviderError(_logger, key, "GET", ex.Message, ex);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentNullException.ThrowIfNull(value);

        TimeSpan validFor = CalculateValidForTimeSpan(options);
        LogCacheSetAsync(_logger, key, typeof(T).Name, DateTimeOffset.UtcNow + validFor, validFor);

        try
        {
            string serializedValue;
            try
            {
                serializedValue = JsonSerializer.Serialize(value, _serializerOptions);
            }
            catch (JsonException jsonEx)
            {
                LogCacheSerializationError(_logger, key, jsonEx.Message, jsonEx);
                throw;
            }

            // StoreAsync returns bool indicating success.
            bool success = await _memcachedClient.StoreAsync(StoreMode.Set, key, serializedValue, validFor).ConfigureAwait(false);

            if (!success)
            {
                LogCacheProviderError(_logger, key, "SET", "Memcached store operation returned false.", null);
            }
        }
        catch (Exception ex) when (ex is not JsonException)
        {
            LogCacheProviderError(_logger, key, "SET", ex.Message, ex);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        LogCacheRemoveAsync(_logger, key);
        try
        {
            bool result = await _memcachedClient.RemoveAsync(key).ConfigureAwait(false);
            if (!result)
            {
                LogCacheProviderError(_logger, key, "REMOVE", $"Memcached remove operation failed. Key: {key}");
            }
        }
        catch (Exception ex)
        {
            LogCacheProviderError(_logger, key, "REMOVE", ex.Message, ex);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {

        LogCacheExistsAsync(_logger, key);
        try
        {
            object? value = await _memcachedClient.GetAsync<object>(key).ConfigureAwait(false);
            bool exists = value != null;
            if(exists) LogCacheExistsHit(_logger, key); else LogCacheExistsMiss(_logger, key);
            return exists;
        }
        catch (Exception ex)
        {
            LogCacheProviderError(_logger, key, "EXISTS", ex.Message, ex);
            return false;
        }
    }
    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        T? value = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        if (value is not null || (default(T) is null && EqualityComparer<T?>.Default.Equals(value, default(T))))
        {
            if (typeof(T).IsClass || Nullable.GetUnderlyingType(typeof(T)) != null)
            {
                if (value != null || EqualityComparer<T?>.Default.Equals(value, default(T)))
                {
                    return value;
                }
            }
            else
            {
                return value;
            }
        }

        LogCacheGetOrSetFactoryExecuting(_logger, key);
        T? newValue = await factory().ConfigureAwait(false);
        if (newValue != null || (default(T) == null))
        {
            LogCacheGetOrSetFactoryCompleted(_logger, key, newValue?.GetType().Name ?? typeof(T).Name);
            await SetAsync(key, newValue, options, cancellationToken).ConfigureAwait(false);
            return newValue;
        }
        return default;
    }


    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prefix);
        
        LogCacheRemoveByPrefixLimitation(_logger, prefix);
        throw new NotSupportedException("RemoveByPrefix is not efficiently supported by Memcached.");
    }

    private TimeSpan CalculateValidForTimeSpan(CacheEntryOptions? options)
    {
        if (options?.AbsoluteExpiration.HasValue == true)
        {
            TimeSpan ttl = options.AbsoluteExpiration.Value - DateTimeOffset.UtcNow;
            return ttl.TotalSeconds > 0 ? ttl : TimeSpan.Zero;
        }
        if (options?.AbsoluteExpirationRelativeToNow.HasValue == true)
        {
            return options.AbsoluteExpirationRelativeToNow.Value.TotalSeconds > 0 ? options.AbsoluteExpirationRelativeToNow.Value : TimeSpan.Zero;
        }
        if (options?.SlidingExpiration.HasValue == true)
        {
            return options.SlidingExpiration.Value.TotalSeconds > 0 ? options.SlidingExpiration.Value : TimeSpan.FromSeconds(_options.DefaultExpirationSeconds);
        }
        return TimeSpan.FromSeconds(_options.DefaultExpirationSeconds);
    }
}
