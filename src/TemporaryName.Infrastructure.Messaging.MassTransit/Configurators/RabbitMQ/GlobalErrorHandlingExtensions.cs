using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.Messaging.MassTransit.Exceptions;
using TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators.RabbitMQ;

public static class GlobalErrorHandlingExtensions
{
    /// <summary>
    /// Configures global message retry and redelivery policies for the bus.
    /// These policies apply to all receive endpoints unless overridden at the endpoint level.
    /// </summary>
    public static IRabbitMqBusFactoryConfigurator ConfigureGlobalErrorHandling(
        this IRabbitMqBusFactoryConfigurator configurator,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        
        GlobalErrorHandlingOptions? settings = serviceProvider
            .GetRequiredService<IOptionsMonitor<GlobalErrorHandlingOptions>>()?.CurrentValue;

        if (settings is null)
        {
            Error error = new("ConfigurationError", $"{nameof(GlobalErrorHandlingOptions)} is not configured, maybe there is missing json settings in masstransitsettings.json?");
            throw new NotConfiguredException(nameof(GlobalErrorHandlingOptions), error);
        }

        configurator.UseMessageRetry(retryConfigurator =>
        {
            if (settings.ImmediateRetryCount > 0)
            {
                retryConfigurator.Immediate(settings.ImmediateRetryCount);
            }

            if (settings.IncrementalRetryCount > 0 && settings.IncrementalRetryInitialInterval > TimeSpan.Zero)
            {
                retryConfigurator.Incremental(
                    settings.IncrementalRetryCount,
                    settings.IncrementalRetryInitialInterval,
                    settings.IncrementalRetryIntervalStep);
            }

            if (settings.ExponentialRetryCount > 0 && settings.ExponentialMinInterval > TimeSpan.Zero)
            {
                retryConfigurator.Exponential(
                    settings.ExponentialRetryCount,
                    settings.ExponentialMinInterval,
                    settings.ExponentialMaxInterval,
                    settings.ExponentialIntervalDelta);
            }


            if (settings.UseExceptionFilters)
            {
                if (settings.HandledExceptionTypesForRetry.Count != 0)
                {
                    foreach (string typeName in settings.HandledExceptionTypesForRetry)
                    {
                        Type? exceptionType = Type.GetType(typeName);
                        if (exceptionType != null && typeof(Exception).IsAssignableFrom(exceptionType))
                        {
                            retryConfigurator.Handle(exceptionType);
                        }
                        // else: Log warning about unresolvable exception type?
                    }
                }

                foreach (string typeName in settings.IgnoredExceptionTypesForRetry)
                {
                    Type? exceptionType = Type.GetType(typeName);
                    if (exceptionType != null && typeof(Exception).IsAssignableFrom(exceptionType))
                    {
                        retryConfigurator.Ignore(exceptionType);
                    }
                    // else: Log warning about unresolvable exception type?
                }
            }
        });

        if (settings.DelayedRedeliveryIntervals is not null && settings.DelayedRedeliveryIntervals.Count != 0)
        {
            configurator.UseDelayedRedelivery(redeliveryConfigurator =>
            {
                redeliveryConfigurator.Intervals(settings.DelayedRedeliveryIntervals.ToArray());

            });
        }

        return configurator;
    }
}