using System;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

public class GlobalErrorHandlingOptions
    {
        public const string SectionName = "RabbitMqGlobalErrorHandling";

        /// <summary>
        /// Number of immediate in-memory retries before attempting delayed redelivery or faulting.
        /// </summary>
        public int ImmediateRetryCount { get; set; } = 3;

        /// <summary>
        /// Number of incremental in-memory retries after immediate retries.
        /// </summary>
        public int IncrementalRetryCount { get; set; } = 3;

        /// <summary>
        /// Initial interval for incremental retries.
        /// </summary>
        public TimeSpan IncrementalRetryInitialInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Increment step for incremental retries.
        /// </summary>
        public TimeSpan IncrementalRetryIntervalStep { get; set; } = TimeSpan.FromSeconds(10);
        
        /// <summary>
        /// Number of exponential in-memory retries. Set to 0 to disable.
        /// This can be an alternative or addition to incremental retries.
        /// </summary>
        public int ExponentialRetryCount { get; set; } = 0; // Default to 0, use incremental by default

        /// <summary>
        /// Minimum interval for exponential retries.
        /// </summary>
        public TimeSpan ExponentialMinInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Maximum interval for exponential retries.
        /// </summary>
        public TimeSpan ExponentialMaxInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Delta for exponential backoff interval.
        /// </summary>
        public TimeSpan ExponentialIntervalDelta { get; set; } = TimeSpan.FromSeconds(5);


        /// <summary>
        /// List of TimeSpan for delayed redelivery intervals using the message scheduler.
        /// These occur after all in-memory retries have been exhausted.
        /// Example: "00:01:00,00:05:00,00:15:00,01:00:00" (1m, 5m, 15m, 1h)
        /// </summary>
        public List<TimeSpan> DelayedRedeliveryIntervals { get; set; } = new List<TimeSpan>
        {
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromHours(1)
        };

        /// <summary>
        /// If true, an exception filter will be added to ignore specific exception types during retry.
        /// This means these exceptions will immediately fault the message instead of retrying.
        /// </summary>
        public bool UseExceptionFilters { get; set; } = true;

        /// <summary>
        /// Fully qualified names of exception types to ignore for retries (will fault immediately).
        /// Example: ["System.ArgumentNullException", "YourProject.Domain.NonTransientDomainException"]
        /// </summary>
        public List<string> IgnoredExceptionTypesForRetry { get; set; } = new List<string>
        {
            typeof(ArgumentNullException).FullName!,
            typeof(ArgumentException).FullName!,
            typeof(NotImplementedException).FullName!,
            typeof(NotSupportedException).FullName!,
            // Add other non-transient or business rule validation exception types here
        };

         /// <summary>
        /// Fully qualified names of exception types to specifically handle for retries.
        /// If this list is populated and UseExceptionFilters is true, only these exceptions will be retried.
        /// Others will fault immediately. If empty, all exceptions not in IgnoredExceptionTypesForRetry are retried.
        /// Example: ["System.Net.Http.HttpRequestException", "Npgsql.NpgsqlException"]
        /// </summary>
        public List<string> HandledExceptionTypesForRetry { get; set; } = new List<string>();
    }
