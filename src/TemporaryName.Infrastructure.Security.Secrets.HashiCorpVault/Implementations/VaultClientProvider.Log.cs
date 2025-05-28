using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Implementations;

public partial class VaultClientProvider
{
    private const int ClassId = 10;
    private const int BaseEventId = Logging.HashiCorpVaultBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int AttemptingToCreateClient = BaseEventId + 0;
    public const int ClientCreatedSuccessfully = BaseEventId + 1;
    public const int ClientCreationFailure = BaseEventId + 2;
    public const int ConfiguringAuthMethod = BaseEventId + 3;
    public const int AuthMethodDetails = BaseEventId + 4;
    public const int SslVerificationSkipped = BaseEventId + 5;
    public const int CustomCaConfigured = BaseEventId + 6;
    public const int VaultConfigurationError = BaseEventId + 7;

    [LoggerMessage(EventId = AttemptingToCreateClient, Level = LogLevel.Information, Message = "VaultClientProvider: Attempting to create Vault client. Address: {VaultAddress}, AuthMethod: {AuthMethod}.")]
    public static partial void LogAttemptingToCreateClient(ILogger logger, string vaultAddress, string authMethod);

    [LoggerMessage(EventId = ClientCreatedSuccessfully, Level = LogLevel.Information, Message = "VaultClientProvider: Vault client created successfully. Address: {VaultAddress}, AuthMethod: {AuthMethod}.")]
    public static partial void LogClientCreatedSuccessfully(ILogger logger, string vaultAddress, string authMethod);

    [LoggerMessage(EventId = ClientCreationFailure, Level = LogLevel.Critical, Message = "VaultClientProvider: Failed to create Vault client. Code: {ErrorCode}, Error: {ErrorMessage}.")]
    public static partial void LogClientCreationFailure(ILogger logger, string errorCode, string? errorMessage, Exception ex);

    [LoggerMessage(EventId = ConfiguringAuthMethod, Level = LogLevel.Debug, Message = "VaultClientProvider: Configuring authentication method: {AuthMethod}.")]
    public static partial void LogConfiguringAuthMethod(ILogger logger, string authMethod);

    [LoggerMessage(EventId = AuthMethodDetails, Level = LogLevel.Debug, Message = "VaultClientProvider: Auth method '{AuthMethod}' details: {Details}.")]
    public static partial void LogAuthMethodDetails(ILogger logger, string authMethod, string details);

    [LoggerMessage(EventId = SslVerificationSkipped, Level = LogLevel.Warning, Message = "VaultClientProvider: SSL/TLS verification is SKIPPED for Vault client. This is INSECURE and for development/testing ONLY.")]
    public static partial void LogSslVerificationSkipped(ILogger logger);

    [LoggerMessage(EventId = CustomCaConfigured, Level = LogLevel.Debug, Message = "VaultClientProvider: Custom CA certificate configured for Vault client from path: {CaPath}.")]
    public static partial void LogCustomCaConfigured(ILogger logger, string caPath);

    [LoggerMessage(EventId = VaultConfigurationError, Level = LogLevel.Error, Message = "VaultClientProvider Configuration Error: Code='{ErrorCode}', Description='{ErrorDescription}'.")]
    public static partial void LogVaultConfigurationError(ILogger logger, string errorCode, string? errorDescription, Exception? ex);
}