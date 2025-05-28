using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.Messaging.MassTransit.Exceptions;
using TemporaryName.Infrastructure.Messaging.MassTransit.Extensions;
using TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators.RabbitMQ;

public static class RabbitMqHealthCheckExtensions
{
    /// <summary>
    /// Adds a dedicated health check for RabbitMQ connectivity to the IServiceCollection.
    /// This relies on the AspNetCore.HealthChecks.Rabbitmq NuGet package.
    /// </summary>
    public static IServiceCollection AddDedicatedRabbitMqHealthChecks(this IServiceCollection services)
    {
        var tempServiceProvider = services.BuildServiceProvider();

        RabbitMqConnectionOptions? connectionSettings = tempServiceProvider
            .GetRequiredService<IOptionsMonitor<RabbitMqConnectionOptions>>()?.CurrentValue;

        RabbitMqHealthCheckOptions? healthCheckSettings = tempServiceProvider
            .GetRequiredService<IOptionsMonitor<RabbitMqHealthCheckOptions>>()?.CurrentValue;

        if (connectionSettings is null)
        {
            Error error = new("ConfigurationError", $"{nameof(RabbitMqConnectionOptions)} is not configured, maybe there is missing json settings in masstransitsettings.json? Cannot add RabbitMQ health check.");
            throw new NotConfiguredException(nameof(RabbitMqConnectionOptions), error);
        }

        if (healthCheckSettings is null)
        {
            Error error = new("ConfigurationError", $"{nameof(RabbitMqHealthCheckOptions)} is not configured, maybe there is missing json settings in masstransitsettings.json? Cannot add RabbitMQ health check.");
            throw new NotConfiguredException(nameof(RabbitMqHealthCheckOptions), error);
        }

        if (!healthCheckSettings.Enabled)
        {
            return services;
        }

        string hostForHealthCheck = connectionSettings.Host.Contains(',')
            ? connectionSettings.Host.Split(',')[0].Trim()
            : connectionSettings.Host;

        string rabbitMqConnectionString = RabbitMqExtensions.CreateAMQPConnectionString(connectionSettings, connectionSettings.UseSsl, hostForHealthCheck);

        services.AddHealthChecks()
            .AddRabbitMQ(
                name: healthCheckSettings.Name,
                failureStatus: healthCheckSettings.FailureStatus,
                tags: healthCheckSettings.Tags,
                timeout: healthCheckSettings.Timeout
            );

        return services;
    }
}