using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Abstractions;
using TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Exceptions;
using TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Settings;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.Kubernetes;
using VaultSharp.V1.AuthMethods.Token;

namespace TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Implementations;

public sealed partial class VaultClientProvider : IVaultClientProvider // Made partial for logging
{
    private readonly IVaultClient _vaultClient;
    private readonly HashiCorpVaultOptions _options;
    private readonly ILogger<VaultClientProvider> _logger;

    public VaultClientProvider(IOptions<HashiCorpVaultOptions> optionsAccessor, ILogger<VaultClientProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(optionsAccessor);
        _options = optionsAccessor.Value ?? throw new ArgumentNullException(nameof(optionsAccessor.Value), "HashiCorpVaultOptions cannot be null.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        LogAttemptingToCreateClient(_logger, _options.Address, _options.AuthMethod);

        try
        {
            IAuthMethodInfo authMethod = GetAuthMethod();

            var vaultClientSettings = new VaultClientSettings(_options.Address, authMethod)
            {
                Namespace = _options.Namespace,

            };

            if (_options.ClientTimeoutSeconds > TimeSpan.Zero)
            {
                vaultClientSettings.VaultServiceTimeout = _options.ClientTimeoutSeconds;
            }

            _vaultClient = new VaultClient(vaultClientSettings);
            LogClientCreatedSuccessfully(_logger, _options.Address, _options.AuthMethod);
        }
        catch (Exception ex) when (ex is not VaultConfigurationException)
        {
            var error = new Error("Vault.Client.InitializationFailed", $"Failed to initialize VaultClient for address '{_options.Address}' using auth method '{_options.AuthMethod}'.");
            LogClientCreationFailure(_logger, error.Code, error.Description, ex);
            throw new VaultConfigurationException(error, ex);
        }
    }

    private IAuthMethodInfo GetAuthMethod()
    {
        LogConfiguringAuthMethod(_logger, _options.AuthMethod);
        switch (_options.AuthMethod.ToLowerInvariant())
        {
            case "token":
                if (string.IsNullOrWhiteSpace(_options.Token))
                {
                    var error = new Error("Vault.Auth.MissingToken", "Token authentication method selected, but Vault token is not configured.");
                    LogVaultConfigurationError(_logger, error.Code, error.Description, null);
                    throw new VaultConfigurationException(error);
                }
                LogAuthMethodDetails(_logger, "Token", $"Token: [REDACTED]");
                return new TokenAuthMethodInfo(_options.Token);

            case "approle":
                if (string.IsNullOrWhiteSpace(_options.AppRoleId) || string.IsNullOrWhiteSpace(_options.AppRoleSecretId))
                {
                    var error = new Error("Vault.Auth.MissingAppRoleCredentials", "AppRole authentication method selected, but Role ID or Secret ID is missing.");
                    LogVaultConfigurationError(_logger, error.Code, error.Description, null);
                    throw new VaultConfigurationException(error);
                }
                LogAuthMethodDetails(_logger, "AppRole", $"RoleId: {_options.AppRoleId}, SecretId: [REDACTED], MountPoint: {_options.AppRoleMountPoint}");
                return new AppRoleAuthMethodInfo(_options.AppRoleId, _options.AppRoleSecretId, _options.AppRoleMountPoint);

            case "kubernetes":
                if (string.IsNullOrWhiteSpace(_options.KubernetesRoleName))
                {
                    var error = new Error("Vault.Auth.MissingKubernetesRole", "Kubernetes authentication method selected, but Kubernetes role name is not configured.");
                    LogVaultConfigurationError(_logger, error.Code, error.Description, null);
                    throw new VaultConfigurationException(error);
                }
                LogAuthMethodDetails(_logger, "Kubernetes", $"Role: {_options.KubernetesRoleName}, TokenPath: {_options.KubernetesTokenPath}, MountPoint: {_options.KubernetesAuthMountPoint}");
                // VaultSharp will read the token from the path
                return new KubernetesAuthMethodInfo(_options.KubernetesRoleName, _options.KubernetesTokenPath, _options.KubernetesAuthMountPoint);

            default:
                var defaultError = new Error("Vault.Auth.UnsupportedMethod", $"Unsupported Vault authentication method: '{_options.AuthMethod}'. Supported methods are Token, AppRole, Kubernetes.");
                LogVaultConfigurationError(_logger, defaultError.Code, defaultError.Description, null);
                throw new VaultConfigurationException(defaultError);
        }
    }

    public IVaultClient GetClient() => _vaultClient;
}
