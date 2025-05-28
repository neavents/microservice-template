using System;
using VaultSharp;

namespace TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Abstractions;

public interface IVaultClientProvider
{
    public IVaultClient GetClient();
}
