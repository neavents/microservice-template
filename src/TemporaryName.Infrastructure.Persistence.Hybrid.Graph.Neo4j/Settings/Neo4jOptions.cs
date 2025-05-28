using System;
using System.ComponentModel.DataAnnotations;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j.Settings;

public class Neo4jOptions
{
public const string SectionName = "Persistence:Neo4j";

    [Required(AllowEmptyStrings = false)]
    public string Uri { get; set; } = string.Empty;

    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Database { get; set; } 

    /// <summary>
    /// If true, the driver will attempt to use an encrypted connection.
    /// For AuraDB or URIs starting with "neo4j+s://", this should be true.
    /// For local development without SSL, set to false.
    /// Production environments should always use encryption.
    /// </summary>
    public bool UseEncryption { get; set; } = false;

    /// <summary>
    /// Specifies the trust strategy for encrypted connections.
    /// Options:
    /// - "TrustSystemCaSignedCertificates": (Default for URI scheme neo4j+s) Trusts certificates signed by a public CA.
    /// - "TrustCustomCaSignedCertificates": Trusts certificates signed by CAs provided in <see cref="TrustedCertificatePath"/>.
    /// - "TrustAllCertificates": (INSECURE - Dev/Test ONLY) Trusts any certificate.
    /// If empty or null, the driver's default based on URI scheme and OS might apply.
    /// </summary>
    public string? TrustStrategy { get; set; }

    /// <summary>
    /// Path to a file or directory containing trusted CA certificates (e.g., .pem files).
    /// Required if <see cref="TrustStrategy"/> is "TrustCustomCaSignedCertificates".
    /// </summary>
    public string? TrustedCertificatePath { get; set; } // e.g., "/etc/ssl/certs/my-custom-ca.pem"

    public int MaxConnectionPoolSize { get; set; } = 100;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan SessionAcquisitionTimeout { get; set; } = TimeSpan.FromSeconds(60);
}
