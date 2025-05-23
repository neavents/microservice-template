using System;

namespace TemporaryName.Infrastructure.Observability.Settings;

/// <summary>
/// Configures the Serilog sink for sending logs to a centralized Elasticsearch cluster.
/// </summary>
public class ElasticsearchSinkOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the Elasticsearch sink is enabled.
    /// If false, logs will not be sent to Elasticsearch even if other settings are configured.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the URIs of your centralized Elasticsearch cluster nodes.
    /// Example: ["http://elasticsearch-node1:9200", "http://elasticsearch-node2:9200"]
    /// At least one URI is required if the sink is enabled.
    /// </summary>
    public string[]? NodeUris { get; set; }

    /// <summary>
    /// Gets or sets the API Key for authenticating with Elasticsearch (e.g., for Elastic Cloud).
    /// This is a secure way to authenticate without embedding username/password.
    /// If ApiKey is provided, Username/Password are typically ignored.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the username for Basic Authentication with Elasticsearch.
    /// Used if ApiKey is not provided.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for Basic Authentication with Elasticsearch.
    /// Used if ApiKey is not provided and Username is present.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the format for Elasticsearch index names.
    /// Uses standard C# DateTime formatting for date-based rolling indices.
    /// Example: "myservice-logs-{0:yyyy.MM.dd}" will create indices like "myservice-logs-2023.10.27".
    /// Defaults to "temporaryname-logs-{0:yyyy.MM.dd}".
    /// </summary>
    public string IndexFormat { get; set; } = "temporaryname-logs-{0:yyyy.MM.dd}";

    /// <summary>
    /// Gets or sets the connection timeout for establishing a connection to Elasticsearch nodes.
    /// Default is 5 seconds. Format: "00:00:05".
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Gets or sets the batch posting limit. The sink will attempt to post logs in batches.
    /// This is the maximum number of events to include in a single batch.
    /// Default is 50.
    /// </summary>
    public int BatchPostingLimit { get; set; } = 50;

    /// <summary>
    /// Gets or sets the period at which the sink will attempt to flush queued log events to Elasticsearch,
    /// regardless of batch size. Default is 2 seconds. Format: "00:00:02".
    /// </summary>
    public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets the minimum log event level required for events to be written to this sink.
    /// Overrides the global minimum level for this specific sink.
    /// Valid values are: Verbose, Debug, Information, Warning, Error, Fatal.
    /// Defaults to "Information".
    /// </summary>
    public string MinimumLogEventLevel { get; set; } = LogEventLevel.Information.ToString();

    /// <summary>
    /// Gets or sets a value indicating whether to register a health check for this Elasticsearch sink.
    /// This helps in monitoring if the application can successfully connect and send logs to Elasticsearch.
    /// </summary>
    public bool RegisterHealthCheck { get; set; } = true;

    // Note: Settings like AutoRegisterTemplate, TemplateName, NumberOfShards, NumberOfReplicas
    // are typically managed centrally on the Elasticsearch cluster or by a one-time setup,
    // not configured per client/microservice for a centralized logging setup.
    // If your centralized ES requires a specific template name to be known by the sink for index pattern matching,
    // TemplateName might still be relevant but AutoRegisterTemplate would usually be false from client side.
}
