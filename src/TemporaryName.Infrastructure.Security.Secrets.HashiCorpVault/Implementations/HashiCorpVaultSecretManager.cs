using System;
using Microsoft.Extensions.Logging;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Abstractions;
using TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Exceptions;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.Commons;

namespace TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Implementations;

public sealed partial class HashiCorpVaultSecretManager : ISecretManager
{
    private readonly IVaultClient _vaultClient;
    private readonly ILogger<HashiCorpVaultSecretManager> _logger;

    public HashiCorpVaultSecretManager(IVaultClientProvider vaultClientProvider, ILogger<HashiCorpVaultSecretManager> logger)
    {
        ArgumentNullException.ThrowIfNull(vaultClientProvider);
        _vaultClient = vaultClientProvider.GetClient() ?? throw new InvalidOperationException("IVaultClientProvider returned a null IVaultClient.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string?> GetSecretAsync(string path, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        LogAttemptingToGetSecret(_logger, path, key);
        try
        {
            Secret<SecretData>? kv2Secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path);
            if (kv2Secret?.Data?.Data != null && kv2Secret.Data.Data.TryGetValue(key, out object? value))
            {
                LogSecretRetrieved(_logger, path, key);
                return value?.ToString();
            }
            LogSecretOrKeyNotFound(_logger, path, key);
            return null;
        }
        catch (VaultApiException vae) when (vae.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            LogSecretPathNotFound(_logger, path, vae);
            return null;
        }
        catch (Exception ex)
        {
            var error = new Error("Vault.Secret.RetrievalFailed", $"Failed to retrieve secret for path '{path}', key '{key}'.");
            LogSecretRetrievalError(_logger, path, key, error.Code, error.Description, ex);
            throw new VaultAccessException(error, ex);
        }
    }

    public async Task<Dictionary<string, string>?> GetSecretsAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        LogAttemptingToGetAllSecrets(_logger, path);
        try
        {
            Secret<SecretData>? kv2Secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path);
            if (kv2Secret?.Data?.Data != null)
            {
                var stringSecrets = kv2Secret.Data.Data
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty);
                LogAllSecretsRetrieved(_logger, path, stringSecrets.Count);
                return stringSecrets;
            }
            LogSecretPathOrNoData(_logger, path);
            return null;
        }
        catch (VaultApiException vae) when (vae.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            LogSecretPathNotFound(_logger, path, vae);
            return null;
        }
        catch (Exception ex)
        {
            var error = new Error("Vault.Secrets.RetrievalFailed", $"Failed to retrieve all secrets for path '{path}'.");
             LogSecretRetrievalError(_logger, path, "[ALL_KEYS]", error.Code, error.Description, ex);
            throw new VaultAccessException(error, ex);
        }
    }
}
