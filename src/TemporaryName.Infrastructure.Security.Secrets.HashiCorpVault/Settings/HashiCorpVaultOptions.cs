using System;
using System.ComponentModel.DataAnnotations;

namespace TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Settings;

public class HashiCorpVaultOptions
{
    public const string SectionName = "Security:HashiCorpVault";

    [Required(AllowEmptyStrings = false)]
    public string Address { get; set; } = "http://localhost:8200"; // Vault server address

    /// <summary>
    /// Authentication method.
    /// Supported: "Token", "AppRole", "Kubernetes".
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string AuthMethod { get; set; } = "Token";

    // Token Auth
    public string? Token { get; set; } // For Token auth method (e.g., VAULT_TOKEN env var)

    // AppRole Auth
    public string? AppRoleId { get; set; }
    public string? AppRoleSecretId { get; set; }
    public string AppRoleMountPoint { get; set; } = "approle";

    // Kubernetes Auth
    public string? KubernetesRoleName { get; set; }
    public string KubernetesTokenPath { get; set; } = "/var/run/secrets/kubernetes.io/serviceaccount/token";
    public string KubernetesAuthMountPoint { get; set; } = "kubernetes";

    /// <summary>
    /// Default timeout for Vault client operations.
    /// </summary>
    public TimeSpan ClientTimeoutSeconds { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Namespace to use for Vault Enterprise. Leave empty for OSS or if not using namespaces.
    /// </summary>
    public string? Namespace { get; set; }

}
