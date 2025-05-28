using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Abstractions;
using TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Implementations;
using TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Settings;

namespace TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault;

public static partial class DependencyInjection
{
    public static IServiceCollection AddHashiCorpVaultSecrets(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var tempSp = services.BuildServiceProvider();
    
        LogStartingRegistration(logger);

        services.AddOptions<HashiCorpVaultOptions>()
            .Bind(configuration.GetSection(HashiCorpVaultOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        LogOptionsConfigured(logger, nameof(HashiCorpVaultOptions), HashiCorpVaultOptions.SectionName);

        // VaultClientProvider manages the VaultClient lifecycle based on options
        services.AddSingleton<IVaultClientProvider, VaultClientProvider>();
        LogProviderRegistered(logger, nameof(IVaultClientProvider), nameof(VaultClientProvider), "Singleton");

        // SecretManager uses the client provider
        services.AddScoped<ISecretManager, HashiCorpVaultSecretManager>();
        LogManagerRegistered(logger, nameof(ISecretManager), nameof(HashiCorpVaultSecretManager), "Scoped");

        LogRegistrationCompleted(logger);
        return services;
    }
}
