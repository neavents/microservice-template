// src/TemporaryName.Infrastructure.Messaging.MassTransit/Settings/ConsumerRetryOptions.cs
namespace TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

public enum RetryStrategy
{
    None,
    Immediate,
    Interval,
    Incremental,
    Exponential
}

public class ConsumerRetryOptions
{
    /// <summary>
    /// Specifies the retry strategy to use. Default is Incremental.
    /// </summary>
    public RetryStrategy Strategy { get; set; } = RetryStrategy.Incremental;

    /// <summary>
    /// Maximum number of times a message processing will be retried.
    /// Applies to Immediate, Interval, Incremental, Exponential strategies.
    /// </summary>
    public int RetryLimit { get; set; } = 5;

    // --- Settings for Immediate Strategy ---
    // (No specific interval settings, just uses RetryLimit)

    // --- Settings for Interval Strategy ---
    /// <summary>
    /// Fixed intervals (in milliseconds) between retry attempts.
    /// Example: [100, 500, 1000] for 3 retries with specific delays.
    /// If shorter than RetryLimit, the last interval is repeated.
    /// </summary>
    public int[] IntervalScheduleMs { get; set; } = [100, 500, 1000, 2000, 5000];

    // --- Settings for Incremental Strategy ---
    /// <summary>
    /// Initial interval (in milliseconds) for the first retry.
    /// </summary>
    public int IncrementalInitialIntervalMs { get; set; } = 200;

    /// <summary>
    /// Amount (in milliseconds) by which the interval increases for each subsequent retry.
    /// </summary>
    public int IncrementalIntervalIncrementMs { get; set; } = 500;

    // --- Settings for Exponential Strategy ---
    /// <summary>
    /// Minimum interval (in milliseconds) for the exponential backoff.
    /// </summary>
    public int ExponentialMinIntervalMs { get; set; } = 100;

    /// <summary>
    /// Maximum interval (in milliseconds) for the exponential backoff.
    /// </summary>
    public int ExponentialMaxIntervalMs { get; set; } = 60000; // 1 minute

    /// <summary>
    /// Factor by which the interval increases for exponential backoff (e.g., 2 for doubling).
    /// </summary>
    public double ExponentialFactor { get; set; } = 2.0;

    /// <summary>
    /// Optional: Policy name for Polly integration, if using Polly for more advanced retry scenarios
    /// (MassTransit's built-in retry is usually sufficient).
    /// </summary>
    public string? PollyPolicyName { get; set; }

    /// <summary>
    /// List of fully qualified exception type names that should trigger a retry.
    /// If empty or null, all exceptions might be retried (depending on IgnoreList).
    /// Example: ["System.Net.Http.HttpRequestException", "MyApp.TransientDbException"]
    /// </summary>
    public List<string> HandleExceptionTypes { get; set; } = new();

    /// <summary>
    /// List of fully qualified exception type names that should NOT trigger a retry and may lead to DLQ directly.
    /// Example: ["MyApp.ValidationException", "MyApp.NonRetryableBusinessException"]
    /// </summary>
    public List<string> IgnoreExceptionTypes { get; set; } = new();
}