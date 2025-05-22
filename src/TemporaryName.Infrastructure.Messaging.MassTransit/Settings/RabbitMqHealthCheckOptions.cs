using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

public class RabbitMqHealthCheckOptions
{
    public const string SectionName = "RabbitMqHealthCheck";

    /// <summary>
    /// Enables or disables the RabbitMQ health check.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Name of the health check as it will appear in the health check results.
    /// </summary>
    public string Name { get; set; } = "rabbitmq-connectivity"; // Changed slightly for clarity

    /// <summary>
    /// The health status to report when the check fails.
    /// Options: HealthStatus.Unhealthy, HealthStatus.Degraded, HealthStatus.Healthy
    /// </summary>
    public HealthStatus FailureStatus { get; set; } = HealthStatus.Unhealthy;

    /// <summary>
    /// Tags to associate with this health check. Useful for filtering health check UIs.
    /// </summary>
    public string[] Tags { get; set; } = new[] { "messaging", "rabbitmq", "infrastructure" };

    /// <summary>
    /// Timeout for the health check operation itself.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
