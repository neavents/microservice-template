using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators.RabbitMQ;

public static class EndpointQuorumQueueExtensions
    {
        /// <summary>
        /// Configures a RabbitMQ receive endpoint to be a Quorum Queue based on settings.
        /// </summary>
        public static void ConfigureQuorumQueue(
            this IRabbitMqReceiveEndpointConfigurator endpointConfigurator,
            string endpointName,
            IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(endpointConfigurator);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endpointName);

            EndpointQuorumQueueOptions? settings = serviceProvider
                .GetRequiredService<IOptionsMonitor<EndpointQuorumQueueOptions>>()?.CurrentValue;

            if (settings is null)
            {
                return;
            }

            bool shouldBeQuorum = settings.DeclareAllAsQuorum;

            if (!shouldBeQuorum && settings.SpecificQuorumEndpoints.Count != 0)
            {
                if (settings.SpecificQuorumEndpoints.Contains(endpointName, StringComparer.OrdinalIgnoreCase))
                {
                    shouldBeQuorum = true;
                }
            }

            if (!shouldBeQuorum && !string.IsNullOrWhiteSpace(settings.QuorumEndpointSuffix))
            {
                if (endpointName.EndsWith(settings.QuorumEndpointSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    shouldBeQuorum = true;
                }
            }

            if (shouldBeQuorum)
            {
                endpointConfigurator.SetQuorumQueue();

                // Apply delivery limit if specified
                if (settings.DeliveryLimit.HasValue && settings.DeliveryLimit > 0)
                {
                    endpointConfigurator.SetQueueArgument("x-delivery-limit", settings.DeliveryLimit.Value);
                }
            }
        }
    }
