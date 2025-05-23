using System;

namespace TemporaryName.Infrastructure.Observability.Settings;

/// <summary>
/// Configures the Elastic Application Performance Monitoring (APM) agent.
/// The agent collects detailed performance metrics and distributed traces.
/// </summary>
public class ElasticApmOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the Elastic APM agent is enabled.
    /// If false, APM data will not be collected or sent.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the URL of your APM Server.
    /// Example: "http://apm-server:8200"
    /// </summary>
    public string? ServerUrl { get; set; }

    /// <summary>
    /// Gets or sets a secret token for authenticating with the APM Server.
    /// Used if the APM Server is configured to require it.
    /// </summary>
    public string? SecretToken { get; set; }

    /// <summary>
    /// Gets or sets an API Key for authenticating with the APM Server (alternative to SecretToken).
    /// This is often a base64 encoded string of "id:api_key".
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the environment name for this service, as reported to APM.
    /// This can override the global ServiceInfo.DeploymentEnvironment for APM-specific tagging.
    /// If not set, APM agent might use ServiceInfo.DeploymentEnvironment or ASPNETCORE_ENVIRONMENT.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the sampling rate for APM transactions (0.0 to 1.0).
    /// 1.0 means sample all transactions. 0.1 means sample 10%.
    /// Can be a string like "1.0" or a double.
    /// Defaults to "1.0".
    /// </summary>
    public string TransactionSampleRate { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets a value indicating whether to enable APM Central Configuration.
    /// If enabled, the agent can fetch configuration updates from Kibana.
    /// Requires APM Server and Kibana to be configured for central configuration.
    /// Set to "true" or "false" as a string, or leave null to use agent default.
    /// </summary>
    public string? CentralConfig { get; set; }

    /// <summary>
    /// Gets or sets the cloud provider information for metadata enrichment.
    /// Valid values: "auto", "aws", "gcp", "azure", "none".
    /// "auto" allows the agent to attempt auto-detection.
    /// </summary>
    public string CloudProvider { get; set; } = "auto";

    /// <summary>
    /// Gets or sets a dictionary of global labels to be added to all APM events (transactions, spans, errors).
    /// Example: { "team": "backend-alpha", "data_sensitivity": "high" }
    /// </summary>
    public Dictionary<string, string> GlobalLabels { get; set; } = new();

    /// <summary>
    /// Gets or sets the log level for the APM agent's internal logging.
    /// Helps in diagnosing issues with the agent itself.
    /// Valid values: "Trace", "Debug", "Information", "Warning", "Error", "Critical", "None".
    /// Defaults to "Warning" or "Error" in most agents.
    /// </summary>
    public string LogLevel { get; set; } = "Warning";

    // Note: Specific instrumentations (AspNetCore, HttpClient, EFCore, MassTransit)
    // are typically enabled by default when using Elastic.Apm.NetCoreAll.
    // Fine-grained control can be achieved by listing specific disabled instrumentations if needed.
    // "ElasticApm:DisabledInstrumentations": "HttpClient,EntityFrameworkCore"
}
