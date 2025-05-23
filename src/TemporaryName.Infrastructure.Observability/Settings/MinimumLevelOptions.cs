using System;

namespace TemporaryName.Infrastructure.Observability.Settings;

/// <summary>
/// Defines the minimum logging levels for Serilog.
/// </summary>
public class MinimumLevelOptions
{
    /// <summary>
    /// Gets or sets the default minimum log event level.
    /// Valid values are: Verbose, Debug, Information, Warning, Error, Fatal.
    /// Defaults to "Information".
    /// </summary>
    public string Default { get; set; } = LogEventLevel.Information.ToString();

    /// <summary>
    /// Gets or sets a dictionary to override the minimum log level for specific logging sources (namespaces).
    /// Example: { "Microsoft.AspNetCore": "Warning", "TemporaryName.MySpecificModule": "Debug" }
    /// </summary>
    public Dictionary<string, string> Override { get; set; } = new();
}
