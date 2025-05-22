using System;
using System.ComponentModel.DataAnnotations;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

/// <summary>
/// Configuration for MassTransit Kill Switch middleware.
/// A Kill Switch stops a receive endpoint if too many messages fail processing.
/// </summary>
public class KillSwitchOptions
{
    /// <summary>
    /// Enables or disables the Kill Switch functionality. Default is false.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The number of messages that must be consumed before the kill switch activates.
    /// This prevents the kill switch from activating when an endpoint first starts.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "ActivationThreshold must be a positive integer.")]
    public int ActivationThreshold { get; set; } = 10;

    /// <summary>
    /// The percentage of messages that must fail (between 0.0 and 1.0) to trip the kill switch.
    /// For example, 0.15 means 15% of messages failed.
    /// </summary>
    [Range(0.01, 1.0, ErrorMessage = "TripThreshold must be between 0.01 (1%) and 1.0 (100%).")]
    public double TripThreshold { get; set; } = 0.15;

    /// <summary>
    /// The minimum number of messages that must fail within the tracking window to trip the kill switch,
    /// regardless of the TripThreshold. Helps prevent tripping on very low message volume.
    /// Default is null (no minimum).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "MinimumFaults must be a positive integer if specified.")]
    public int? MinimumFaults { get; set; }


    /// <summary>
    /// The duration of the tracking window for fault rates.
    /// Example: "00:01:00" for one minute.
    /// </summary>
    [RegularExpression(@"^\d{2}:\d{2}:\d{2}$", ErrorMessage = "TrackingPeriod must be in 'hh:mm:ss' format.")]
    public string TrackingPeriod { get; set; } = "00:01:00"; // Default: 1 minute

    /// <summary>
    /// The duration the endpoint will remain stopped before attempting to restart.
    /// Example: "00:05:00" for five minutes.
    /// </summary>
    [RegularExpression(@"^\d{2}:\d{2}:\d{2}$", ErrorMessage = "RestartTimeout must be in 'hh:mm:ss' format.")]
    public string RestartTimeout { get; set; } = "00:05:00"; // Default: 5 minutes

    /// <summary>
    /// Optional: The specific exception types that should be considered faults by the kill switch.
    /// If empty, all exceptions are considered faults. Provide FullName of exception types.
    /// </summary>
    public List<string> FilterExceptionTypes { get; set; } = new();


    // Helper to parse TimeSpan for internal use
    internal TimeSpan GetTrackingPeriod() => TimeSpan.TryParse(TrackingPeriod, out var ts) ? ts : TimeSpan.FromMinutes(1);
    internal TimeSpan GetRestartTimeout() => TimeSpan.TryParse(RestartTimeout, out var ts) ? ts : TimeSpan.FromMinutes(5);
}