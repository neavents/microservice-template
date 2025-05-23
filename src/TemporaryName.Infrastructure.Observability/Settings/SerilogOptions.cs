using System;

namespace TemporaryName.Infrastructure.Observability.Settings;

/// <summary>
/// Configures Serilog's behavior, including minimum log levels and console output.
/// </summary>
public class SerilogOptions
{
    /// <summary>
    /// Gets or sets the minimum log level settings for Serilog.
    /// </summary>
    public MinimumLevelSettings MinimumLevel { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to write logs to the console.
    /// Recommended to be true for local development and potentially for containerized environments
    /// where logs are captured from stdout/stderr.
    /// </summary>
    public bool WriteToConsole { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable Serilog's internal diagnostic logging (SelfLog).
    /// When enabled, Serilog will output its own troubleshooting messages to Console.Error.
    /// This should typically be disabled in production unless diagnosing Serilog issues.
    /// </summary>
    public bool EnableSelfLog { get; set; } = false;

    /// <summary>
    /// Gets or sets default properties to be added to all log events.
    /// This can be used for static tags or context that applies globally to the service.
    /// Example: { "Region": "us-west-1", "DataCenter": "dc1" }
    /// </summary>
    public Dictionary<string, string> DefaultLogProperties { get; set; } = new();
}
