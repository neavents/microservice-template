using System;
using Autofac;

namespace TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault;

public class HashiCorpVaultModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Most registrations are handled by IServiceCollection extensions.
        // Add Autofac-specific registrations here if needed.
        // For instance, if you had specific interceptors for ISecretManager
        // that were easier to register with Autofac's dynamic proxy.
    }
}
