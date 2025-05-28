using System;
using System.ComponentModel.DataAnnotations;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra.Settings;

public class CassandraOptions
{
    public const string SectionName = "Persistence:Cassandra";

    [Required(AllowEmptyStrings = false)]
    public string ContactPoints { get; set; } = string.Empty; // Comma-separated: "host1,host2"

    public int Port { get; set; } = 9042; 

    [Required(AllowEmptyStrings = false)]
    public string DefaultKeyspace { get; set; } = string.Empty;

    public string? Username { get; set; }
    public string? Password { get; set; }

    public string? LocalDatacenter { get; set; } 

    public int MaxConnectionsPerHost { get; set; } = 8; 
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(12);

    public bool UseSsl { get; set; } = false;
    // Add other SSL/TLS options if needed (e.g., certificate paths, expected hostname)
}