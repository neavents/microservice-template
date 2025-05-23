using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TemporaryName.Infrastructure.Caching.Abstractions;
using TemporaryName.Infrastructure.Caching.Abstractions.Constants;
using TemporaryName.Infrastructure.Caching.Redis.Implementations;
using TemporaryName.Infrastructure.Caching.Redis.Settings;

namespace TemporaryName.Infrastructure.Caching.Redis;

public static partial class DependencyInjection
{
    public static IServiceCollection AddInfrastructureCachingRedis(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        LogStartingRegistration(logger);

        services.AddOptions<RedisCacheOptions>()
            .Bind(configuration.GetSection(RedisCacheOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        LogOptionsConfigured(logger, nameof(RedisCacheOptions), RedisCacheOptions.SectionName);

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            RedisCacheOptions redisOptions = sp.GetRequiredService<IOptions<RedisCacheOptions>>().Value;
            try
            {
                ConfigurationOptions redisConfig = ConfigurationOptions.Parse(redisOptions.ConnectionString, ignoreUnknown: true);
                redisConfig.AbortOnConnectFail = false;

                if (!string.IsNullOrWhiteSpace(redisOptions.Password) && string.IsNullOrWhiteSpace(redisConfig.Password))
                {
                    redisConfig.Password = redisOptions.Password;
                }

                redisConfig.Ssl = redisOptions.Ssl || redisConfig.Ssl;
                if (redisConfig.Ssl)
                {
                    if (!string.IsNullOrWhiteSpace(redisOptions.SslHost) && string.IsNullOrWhiteSpace(redisConfig.SslHost))
                    {
                        redisConfig.SslHost = redisOptions.SslHost;
                    }

                    if (redisOptions.AllowUntrustedCertificate)
                    {
                        LogRedisUntrustedSSLAllowed(logger);
                        redisConfig.CertificateValidation += (sender, certificate, chain, sslPolicyErrors) => true;
                    }
                }

                redisConfig.AllowAdmin = redisOptions.AllowAdmin || redisConfig.AllowAdmin;


                IConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect(redisConfig);
                LogRedisConnectionMultiplexerRegistered(logger, SanitizeConnectionString(redisOptions.ConnectionString));
                return multiplexer;
            }
            catch (RedisConnectionException ex)
            {
                LogRedisConnectionFailed(logger, ex.Message, ex);
                throw new InvalidOperationException($"Failed to connect to Redis: {ex.Message}. Caching will be unavailable.", ex);
            }
        });

        services.AddSingleton<RedisCacheKeyService>();
        services.AddKeyedSingleton<ICacheKeyService, RedisCacheKeyService>(CacheProviderKeys.Redis);
        LogCacheKeyServiceRegistered(logger, nameof(ICacheKeyService), nameof(RedisCacheKeyService), "Singleton");

        services.AddSingleton<RedisCacheService>();
        services.AddKeyedSingleton<ICacheService, RedisCacheService>(CacheProviderKeys.Redis);
        LogCacheServiceRegistered(logger, nameof(ICacheService), nameof(RedisCacheService), "Singleton");

        LogRegistrationCompleted(logger);
        return services;
    }

    private static string SanitizeConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return "EMPTY_OR_NULL";

        try
        {
            ConfigurationOptions options = ConfigurationOptions.Parse(connectionString, false);

            options.Password = string.IsNullOrWhiteSpace(options.Password) ? null : "*****";
            return options.ToString(true);
        }
        catch
        {
            return "CONNECTION_STRING_PARSING_FAILED_OR_INVALID_FORMAT";
        }
    }
}
