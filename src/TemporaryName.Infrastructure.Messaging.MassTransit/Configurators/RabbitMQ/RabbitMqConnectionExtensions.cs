using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.Messaging.MassTransit.Exceptions;
using TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators.RabbitMQ;

public static class RabbitMqConnectionExtensions
{
    /// <summary>
    /// Configures the RabbitMQ host connection details.
    /// </summary>
    public static IRabbitMqBusFactoryConfigurator ConfigureRabbitMqConnection(
        this IRabbitMqBusFactoryConfigurator configurator,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        RabbitMqConnectionOptions? settings = serviceProvider
            .GetRequiredService<IOptionsMonitor<RabbitMqConnectionOptions>>()?.CurrentValue;

        if (settings is null)
        {
            Error error = new("ConfigurationError", $"{nameof(RabbitMqConnectionOptions)} is not configured, maybe there is missing json settings in masstransitsettings.json?");
            throw new NotConfiguredException(nameof(RabbitMqConnectionOptions), error);
        }

        Action<IRabbitMqHostConfigurator> hostConfigAction = hostConfig =>
        {
            hostConfig.Username(settings.Username);
            hostConfig.Password(settings.Password);
            hostConfig.Heartbeat(settings.RequestedHeartbeat);
            hostConfig.ConnectionName(settings.ConnectionName);

            if (settings.UseSsl)
            {
                hostConfig.UseSsl(sslConfig =>
                {
                    sslConfig.Protocol = settings.SslProtocol;
                    if (!string.IsNullOrWhiteSpace(settings.SslServerName))
                    {
                        sslConfig.ServerName = settings.SslServerName;
                    }

                    if (!string.IsNullOrWhiteSpace(settings.SslCertificatePath))
                    {
                        sslConfig.CertificatePath = settings.SslCertificatePath;
                        if (!string.IsNullOrWhiteSpace(settings.SslCertificatePassphrase))
                        {
                            sslConfig.CertificatePassphrase = settings.SslCertificatePassphrase;
                        }
                    }
                    // sslConfig.AllowPolicyErrors();
                });
            }
        };

        if (settings.UseCluster && settings.Host.Contains(','))
        {
            // Connect to a cluster of RabbitMQ nodes
            string[] hosts = settings.Host.Split(',').Select(h => h.Trim()).ToArray();

            configurator.Host(settings.Host, settings.VirtualHost, hostConfigAction);
        }
        else
        {
            // Connect to a single RabbitMQ node or a load balancer
            configurator.Host(settings.Host, settings.Port, settings.VirtualHost, hostConfigAction);
        }

        configurator.ConfigureRabbitMqMessageScheduler(); 

        return configurator;
    }
}
