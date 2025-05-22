using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using TemporaryName.Infrastructure.Messaging.MassTransit.Settings;
using TemporaryName.Infrastructure.Messaging.MassTransit.Exceptions; 

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Configurators;

public static partial class RetryConfigurator
{
    public static void ConfigureConsumerRetries<TEndpointConfigurator>(
        TEndpointConfigurator configurator, // e.g., IReceiveEndpointConfigurator, IKafkaTopicEndpointConfigurator
        ConsumerRetryOptions? retryOptions,
        ILogger logger,
        string transportName)
        where TEndpointConfigurator : IConsumePipeConfigurator // Base interface for UseMessageRetry
    {
        if (retryOptions == null)
        {
            LogRetryOptionsMissing(logger, transportName);
            // No custom retry configuration, MassTransit defaults apply (often none or immediate).
            return;
        }

        if (retryOptions.Strategy == RetryStrategy.None)
        {
            LogRetryStrategyNone(logger, transportName);
            // Explicitly no retries from this configuration.
            // To ensure no retries at all, including MassTransit defaults, might require
            // cfg.UseMessageRetry(r => r.None()); if TEndpointConfigurator allows.
            // However, simply not adding a retry policy often means no retries beyond broker-level.
            return;
        }

        LogRetryStrategyBeingConfigured(logger, retryOptions.Strategy, retryOptions.RetryLimit, transportName);

        configurator.UseMessageRetry(r =>
        {
            // Exception Filtering: Which exceptions trigger a retry?
            if (retryOptions.HandleExceptionTypes != null && retryOptions.HandleExceptionTypes.Count != 0)
            {
                foreach (string exTypeName in retryOptions.HandleExceptionTypes)
                {
                    Type? exType = Type.GetType(exTypeName, throwOnError: false, ignoreCase: true);
                    if (exType != null && typeof(Exception).IsAssignableFrom(exType))
                    {
                        // Dynamically call r.Handle<TException>()
                        var handleMethod = typeof(IRetryConfigurator).GetMethods()
                            .First(m => m.Name == "Handle" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0)
                            .MakeGenericMethod(exType);
                        handleMethod.Invoke(r, null);
                        LogRetryHandlingExceptionType(logger, exType.FullName!, transportName);
                    }
                    else
                    {
                        LogRetryUnknownExceptionTypeToHandle(logger, exTypeName, transportName);
                    }
                }
            }
            else // Default: retry on all exceptions unless explicitly ignored.
            {
                r.Handle<Exception>(ex => !IsIgnoredException(ex, retryOptions, logger, transportName));
                LogRetryHandlingAllNonIgnoredExceptions(logger, transportName);
            }

            // Which exceptions should NOT trigger a retry (and potentially go to DLQ faster)?
            // This is implicitly handled by the filter in r.Handle<Exception>(...) above if HandleExceptionTypes is empty.
            // If HandleExceptionTypes is populated, then only those are retried.
            // If both Handle and Ignore are specified, Ignore takes precedence for those specific types.
            if (retryOptions.IgnoreExceptionTypes != null && retryOptions.IgnoreExceptionTypes.Count != 0)
            {
                foreach (string exTypeName in retryOptions.IgnoreExceptionTypes)
                {
                    Type? exType = Type.GetType(exTypeName, throwOnError: false, ignoreCase: true);
                    if (exType != null && typeof(Exception).IsAssignableFrom(exType))
                    {
                        var ignoreMethod = typeof(IRetryConfigurator).GetMethods()
                            .First(m => m.Name == "Ignore" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0)
                            .MakeGenericMethod(exType);
                        ignoreMethod.Invoke(r, null);
                        LogRetryIgnoringExceptionType(logger, exType.FullName!, transportName);
                    }
                    else
                    {
                        LogRetryUnknownExceptionTypeToIgnore(logger, exTypeName, transportName);
                    }
                }
            }

            // Retry Policies
            switch (retryOptions.Strategy)
            {
                case RetryStrategy.Immediate:
                    r.Immediate(retryOptions.RetryLimit);
                    LogRetryImmediateConfigured(logger, retryOptions.RetryLimit, transportName);
                    break;
                case RetryStrategy.Interval:
                    var intervals = retryOptions.IntervalScheduleMs.Select(ms => TimeSpan.FromMilliseconds(ms)).ToArray();
                    if (intervals.Length != 0) // Ensure there are intervals
                    {
                        // For fixed intervals, use the Intervals method. RetryLimit applies to the number of intervals.
                        r.Intervals(intervals.Take(retryOptions.RetryLimit).ToArray()); // Ensure we don't exceed RetryLimit
                    }
                    else
                    {
                        LogRetryIntervalScheduleMissingOrEmpty(logger, transportName);
                        r.None(); // Fallback to no retry if schedule is invalid
                    }
                    LogRetryIntervalConfigured(logger, retryOptions.RetryLimit, string.Join(",", retryOptions.IntervalScheduleMs), transportName);
                    break;
                case RetryStrategy.Incremental:
                    r.Incremental(retryOptions.RetryLimit, TimeSpan.FromMilliseconds(retryOptions.IncrementalInitialIntervalMs), TimeSpan.FromMilliseconds(retryOptions.IncrementalIntervalIncrementMs));
                    LogRetryIncrementalConfigured(logger, retryOptions.RetryLimit, retryOptions.IncrementalInitialIntervalMs, retryOptions.IncrementalIntervalIncrementMs, transportName);
                    break;
                case RetryStrategy.Exponential:
                    // Note: MassTransit's Exponential takes min, max, and interval (delta for growth factor)
                    // The 'Factor' needs to be translated into an interval if it's a multiplier.
                    // For simplicity, using it as interval delta. A true exponential factor might require a custom policy.
                    r.Exponential(retryOptions.RetryLimit,
                                  TimeSpan.FromMilliseconds(retryOptions.ExponentialMinIntervalMs),
                                  TimeSpan.FromMilliseconds(retryOptions.ExponentialMaxIntervalMs),
                                  TimeSpan.FromMilliseconds(retryOptions.ExponentialFactor)); // Assuming Factor is an interval for simplicity here
                    LogRetryExponentialConfigured(logger, retryOptions.RetryLimit, retryOptions.ExponentialMinIntervalMs, retryOptions.ExponentialMaxIntervalMs, retryOptions.ExponentialFactor, transportName);
                    break;
                default: // Should not happen if options are validated
                    LogRetryInvalidStrategy(logger, retryOptions.Strategy.ToString(), transportName);
                    r.None(); // Default to no retry if strategy is somehow invalid
                    break;
            }
        });
        LogRetryPolicyApplied(logger, retryOptions.Strategy, transportName);
    }

    private static bool IsIgnoredException(Exception ex, ConsumerRetryOptions retryOptions, ILogger logger, string transportName)
    {
        if (retryOptions.IgnoreExceptionTypes == null || retryOptions.IgnoreExceptionTypes.Count == 0)
        {
            return false;
        }

        string thrownExceptionTypeName = ex.GetType().FullName!;
        foreach (string ignoreExTypeNameFull in retryOptions.IgnoreExceptionTypes)
        {
            // Allow for partial matches (e.g., namespace) or full name matches.
            // Type.GetType might be too strict if assembly isn't loaded or FQN isn't perfect.
            // A more robust way might be to iterate ex.GetType().GetBaseTypes() as well.
            if (thrownExceptionTypeName.Equals(ignoreExTypeNameFull, StringComparison.OrdinalIgnoreCase) ||
                ex.GetType().Name.Equals(ignoreExTypeNameFull, StringComparison.OrdinalIgnoreCase)) // Simple name match
            {
                LogExceptionTypeIgnoredByPolicy(logger, thrownExceptionTypeName, ignoreExTypeNameFull, transportName);
                return true;
            }
            // Consider checking base types:
            // Type? currentExType = ex.GetType();
            // while (currentExType != null) {
            // if (currentExType.FullName == ignoreExTypeNameFull) return true;
            // currentExType = currentExType.BaseType;
            // }
        }
        return false;
    }
}