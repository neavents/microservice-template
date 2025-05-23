using System;

namespace TemporaryName.Infrastructure.Observability.Settings;

/// <summary>
/// Contains general information about the service, used for enriching observability data.
/// </summary>
public class ServiceInfoOptions
{
    /// <summary>
    /// Gets or sets the explicit name of the service (e.g., "TemporaryName.WebApi", "TemporaryName.OrderProcessorWorker").
    /// If not provided, it's typically auto-detected from the entry assembly.
    /// This is crucial for identifying the service in logs, traces, and metrics.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the version of the service (e.g., "1.0.0", "2.1.0-beta").
    /// If not provided, it's typically auto-detected from the entry assembly's version.
    /// Useful for correlating telemetry with specific deployments.
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Gets or sets the deployment environment (e.g., "Development", "Staging", "Production", "QA").
    /// This is often sourced from the ASPNETCORE_ENVIRONMENT variable.
    /// Essential for filtering and analyzing telemetry data by environment.
    /// </summary>
    public string? DeploymentEnvironment { get; set; }
}
