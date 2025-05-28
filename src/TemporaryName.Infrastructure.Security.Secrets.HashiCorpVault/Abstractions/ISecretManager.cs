using System;

namespace TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Abstractions;

public interface ISecretManager
{
    public Task<string?> GetSecretAsync(string path, string key);
    public Task<Dictionary<string, string>?> GetSecretsAsync(string path);
}
