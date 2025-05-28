using System;

namespace TemporaryName.Infrastructure.Observability.Settings;

/// <summary>
/// Provides centralized configuration for observability features, including logging with Serilog,
/// Elasticsearch sink, and Elastic Application Performance Monitoring (APM).
/// These settings are typically bound from the "Observability" section of appsettings.json.
/// </summary>
public class ObservabilityOptions
{
    /// <summary>
    /// Defines the configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Observability";

    /// <summary>
    /// Gets or sets general service information used for tagging logs and traces.
    /// This helps identify the source of telemetry data in a distributed system.
    /// </summary>
    public ServiceInfoSettings ServiceInfo { get; set; } = new();

    /// <summary>
    /// Gets or sets the configuration for Serilog, the structured logging framework.
    /// </summary>
    public SerilogSettings Serilog { get; set; } = new();

    /// <summary>
    /// Gets or sets the configuration for the Serilog sink that ships logs to your centralized Elasticsearch cluster.
    /// </summary>
    public ElasticsearchSinkSettings ElasticsearchSink { get; set; } = new();

    /// <summary>
    /// Gets or sets the configuration for the Elastic Application Performance Monitoring (APM) agent.
    /// </summary>
    public ElasticApmSettings ElasticApm { get; set; } = new();
}
