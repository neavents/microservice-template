using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.Messaging.MassTransit.Exceptions;
using TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators.RabbitMQ;

public static class EndpointRateLimiterExtensions
{
    /// <summary>
    /// Configures a rate limiter for a RabbitMQ receive endpoint.
    /// </summary>
    public static void ConfigureRateLimiter(
        this IRabbitMqReceiveEndpointConfigurator endpointConfigurator,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        
        EndpointRateLimiterOptions? settings = serviceProvider
            .GetRequiredService<IOptionsMonitor<EndpointRateLimiterOptions>>()?.CurrentValue;
        if (settings is null)
        {
            Error error = new("ConfigurationError", $"{nameof(EndpointRateLimiterOptions)} is not configured, maybe there is missing json settings in masstransitsettings.json?");
            throw new NotConfiguredException(nameof(EndpointRateLimiterOptions), error);
        }
        if (!settings.Enabled)
        {
            return;
        }

        if (settings.MessageLimit <= 0 || settings.Interval <= TimeSpan.Zero)
        {
            Error error = new("ConfigurationError", $"Specified MessageLimit ({settings.MessageLimit}) should be higher than 0 OR Specified Interval time ({settings.Interval}) should be higher than 0.");
            throw new BadConfigurationException(nameof(EndpointRateLimiterOptions), error);
        }

        endpointConfigurator.UseRateLimit(settings.MessageLimit, settings.Interval);
    }
}
