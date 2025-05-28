using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TemporaryName.Infrastructure.Caching.Abstractions;
using TemporaryName.Infrastructure.Caching.Abstractions.Settings;
using TemporaryName.Infrastructure.Caching.Redis.Settings;

namespace TemporaryName.Infrastructure.Caching.Redis.Implementations;

public partial class RedisCacheService : ICacheService 
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly RedisCacheOptions _options;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    public RedisCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<RedisCacheOptions> options,
        ILogger<RedisCacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    private IDatabase GetDatabase() => _connectionMultiplexer.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        LogCacheGetAsync(_logger, key);

        try
        {
            RedisValue redisValue = await GetDatabase().StringGetAsync(key).ConfigureAwait(false);
            if (!redisValue.HasValue)
            {
                LogCacheGetMiss(_logger, key);
                return default;
            }

            try
            {
                T? value = JsonSerializer.Deserialize<T>(redisValue.ToString(), _serializerOptions);
                LogCacheGetHit(_logger, key, typeof(T).Name);
                return value;
            }
            catch (JsonException jsonEx)
            {
                LogCacheDeserializationError(_logger, key, jsonEx.Message, jsonEx);
                return default; // Or rethrow/handle differently
            }
        }
        catch (RedisException redisEx)
        {
            LogCacheProviderError(_logger, key, "GET", redisEx.Message, redisEx);
            // Depending on strategy, might return default or rethrow custom exception
            return default;
        }
        catch (Exception ex)
        {
            LogCacheOperationError(_logger, key, "GET", ex.Message, ex);
            throw; // Or handle more gracefully
        }
    }

    public async Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        options ??= new CacheEntryOptions();
        TimeSpan? expiry = CalculateExpiry(options);

        LogCacheSetAsync(_logger, key, typeof(T).Name, options.AbsoluteExpiration, expiry ?? options.SlidingExpiration);
        
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
                throw; // Fail fast if serialization fails
            }

            await GetDatabase().StringSetAsync(key, serializedValue, expiry, When.Always).ConfigureAwait(false);
        }
        catch (RedisException redisEx)
        {
            LogCacheProviderError(_logger, key, "SET", redisEx.Message, redisEx);
            // Handle or rethrow
        }
        catch (Exception ex) when (ex is not JsonException) // JsonException already handled and rethrown
        {
            LogCacheOperationError(_logger, key, "SET", ex.Message, ex);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        LogCacheRemoveAsync(_logger, key);
        try
        {
            await GetDatabase().KeyDeleteAsync(key).ConfigureAwait(false);
        }
        catch (RedisException redisEx)
        {
            LogCacheProviderError(_logger, key, "REMOVE", redisEx.Message, redisEx);
            // Handle or rethrow
        }
        catch (Exception ex)
        {
            LogCacheOperationError(_logger, key, "REMOVE", ex.Message, ex);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        LogCacheExistsAsync(_logger, key);
        try
        {
            bool exists = await GetDatabase().KeyExistsAsync(key).ConfigureAwait(false);
            if(exists) LogCacheExistsHit(_logger, key); else LogCacheExistsMiss(_logger, key);
            return exists;
        }
        catch (RedisException redisEx)
        {
            LogCacheProviderError(_logger, key, "EXISTS", redisEx.Message, redisEx);
            return false;
        }
        catch (Exception ex)
        {
            LogCacheOperationError(_logger, key, "EXISTS", ex.Message, ex);
            throw;
        }
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentNullException.ThrowIfNull(factory, nameof(factory));

        T? value = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        
        if (value != null || (default(T) == null && EqualityComparer<T?>.Default.Equals(value, default(T)))) // Handles non-null default for value types
        {
            // If T is a non-nullable value type, 'default' is a valid cached value.
            // If T is a reference type or nullable value type, null means cache miss.
            // This condition ensures that if 'null' is a valid cached value for a nullable type, it's returned.
            // The check `(default(T) == null && EqualityComparer<T?>.Default.Equals(value, default(T)))` specifically addresses
            // the scenario where T is a nullable type (e.g. int?) and the cached value is indeed null.
            if (typeof(T).IsClass || Nullable.GetUnderlyingType(typeof(T)) != null)
            {
                if (value != null || EqualityComparer<T?>.Default.Equals(value, default(T)))
                { // hit if value is not null, or value is null and T is nullable
                    return value;
                }
            }
            else
            { // Non-nullable value type
                return value;
            }
        }


        LogCacheGetOrSetFactoryExecuting(_logger, key);
        T? newValue = await factory();

        if (newValue != null || (default(T) == null)) // Only cache if factory returns non-null, or if T itself can be null (e.g. string, nullable struct)
        {
            LogCacheGetOrSetFactoryCompleted(_logger, key, newValue?.GetType().Name ?? typeof(T).Name);
            await SetAsync(key, newValue, options, cancellationToken).ConfigureAwait(false);
            return newValue;
        }
        return default;
    }


    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix, nameof(prefix));
        LogCacheRemoveByPrefixAsync(_logger, prefix);

        try
        {
            List<RedisKey> keysToDelete = new List<RedisKey>();
            // Use SCAN to avoid blocking the server, iterate over all servers in a cluster
            foreach (System.Net.EndPoint endPoint in _connectionMultiplexer.GetEndPoints())
            {
                IServer server = _connectionMultiplexer.GetServer(endPoint);
                // Ensure the prefix includes the instance name if configured
                string actualPrefix = (_options.InstanceName ?? "") + prefix;
                if (server.IsConnected) // Check if server is connected
                {
                     await foreach (RedisKey key in server.KeysAsync(pattern: actualPrefix + "*", pageSize: 1000).WithCancellation(cancellationToken).ConfigureAwait(false))
                     {
                         keysToDelete.Add(key);
                         if (cancellationToken.IsCancellationRequested) break;
                     }
                }
                 if (cancellationToken.IsCancellationRequested) break;
            }
            
            LogCacheRemoveByPrefixScan(_logger, keysToDelete.Count, prefix);

            if (keysToDelete.Count != 0)
            {
                long deletedCount = await GetDatabase().KeyDeleteAsync(keysToDelete.ToArray()).ConfigureAwait(false);
                LogCacheRemoveByPrefixDelete(_logger, deletedCount, prefix);
            }
        }
        catch (RedisException redisEx)
        {
            LogCacheProviderError(_logger, null, $"REMOVE_BY_PREFIX:{prefix}", redisEx.Message, redisEx);
        }
        catch (Exception ex)
        {
            LogCacheOperationError(_logger, null, $"REMOVE_BY_PREFIX:{prefix}", ex.Message, ex);
            throw;
        }
    }


    private TimeSpan? CalculateExpiry(CacheEntryOptions options)
    {
        if (options.AbsoluteExpiration.HasValue)
        {
            return options.AbsoluteExpiration.Value - DateTimeOffset.UtcNow;
        }
        if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            return options.AbsoluteExpirationRelativeToNow.Value;
        }
        if (options.SlidingExpiration.HasValue)
        {
            return options.SlidingExpiration.Value;
        }
    
        if(_options.DefaultAbsoluteExpirationRelativeToNow.HasValue)
        {
            return _options.DefaultAbsoluteExpirationRelativeToNow.Value;
        }
        if(_options.DefaultSlidingExpiration.HasValue)
        {
            return _options.DefaultSlidingExpiration.Value;
        }
        return null;
    }
}