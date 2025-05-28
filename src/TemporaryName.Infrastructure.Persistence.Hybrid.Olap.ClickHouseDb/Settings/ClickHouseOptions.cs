using System;
using System.ComponentModel.DataAnnotations;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Olap.ClickHouseDb.Settings;

public class ClickHouseOptions
{
    public const string SectionName = "Persistence:ClickHouse";

    // ClickHouse.Client uses a connection string format.
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = string.Empty;
    // Example: "Host=localhost;Port=8123;Database=default;User=default;Password=your_password;Compress=True;SocketTimeout=60000;ReadWriteTimeout=300000"
    // For native protocol (port 9000 usually): "Host=localhost;Port=9000..."
    // Check documentation for full connection string parameters.


    public TimeSpan DefaultCommandTimeout { get; set; } = TimeSpan.FromMinutes(5); 

    /// <summary>
    /// Explicitly enable SSL/TLS. This should align with connection string parameters.
    /// Some drivers might infer from scheme (e.g. Port 8443 for HTTPS, 9440 for Native TCP+TLS)
    /// but it's good to be explicit.
    /// </summary>
    public bool UseSsl { get; set; } = false;
}
