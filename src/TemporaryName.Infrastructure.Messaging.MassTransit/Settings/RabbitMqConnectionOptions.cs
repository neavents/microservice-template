using System;
using System.Security.Authentication;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

public class RabbitMqConnectionOptions
{
    public const string SectionName = "RabbitMqConnection";

    /// <summary>
    /// Comma-separated list of RabbitMQ hostnames or IP addresses for clustering.
    /// Example: "rabbitmq-node1,rabbitmq-node2,rabbitmq-node3"
    /// For a single node: "localhost"
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// The virtual host to connect to on the RabbitMQ server.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Username for RabbitMQ authentication.
    /// </summary>
    public string Username { get; set; } = "guest";

    /// <summary>
    /// Password for RabbitMQ authentication.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Port for AMQP connection. Default is 5672. Use 5671 for SSL/TLS.
    /// </summary>
    public ushort Port { get; set; } = 5672;

    /// <summary>
    /// Specifies whether to use SSL/TLS for the connection.
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Optional: Server name for SSL/TLS certificate validation.
    /// Typically the hostname if different from the connection host string.
    /// </summary>
    public string SslServerName { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Path to the client certificate for mTLS authentication.
    /// </summary>
    public string SslCertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Passphrase for the client certificate.
    /// </summary>
    public string SslCertificatePassphrase { get; set; } = string.Empty;

    /// <summary>
    /// Specifies the SslProtocols to use. Defaults to Tls12.
    /// Consider Tls13 if your environment supports it.
    /// </summary>
    public SslProtocols SslProtocol { get; set; } = SslProtocols.Tls12;

    /// <summary>
    /// Heartbeat interval for the connection, in seconds.
    /// Helps detect dead connections.
    /// MassTransit default is 60s, RabbitMQ default is 580s if client requests 0.
    /// Setting to a lower value like 30s can be beneficial.
    /// </summary>
    public ushort RequestedHeartbeat { get; set; } = 30; // MassTransit default is 60s

    /// <summary>
    /// Connection timeout when establishing the connection.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// If true, MassTransit will attempt to connect to multiple hosts specified in the Host property
    /// if they are comma-separated, providing client-side cluster connection.
    /// </summary>
    public bool UseCluster { get; set; } = false;

    public string ConnectionName { get; set; } = "TemporaryName";
}
