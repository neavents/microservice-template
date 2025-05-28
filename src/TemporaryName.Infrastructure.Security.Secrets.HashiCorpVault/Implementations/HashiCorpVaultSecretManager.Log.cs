using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault.Implementations;

public partial class HashiCorpVaultSecretManager
{
    private const int ClassId = 20; 
    private const int BaseEventId = Logging.HashiCorpVaultBaseEventId + (ClassId * Logging.IncrementPerClass);
        public const int AttemptingToGetSecret = BaseEventId + 0;
        public const int SecretRetrieved = BaseEventId + 1;
        public const int SecretOrKeyNotFound = BaseEventId + 2;
        public const int SecretPathNotFound = BaseEventId + 3;
        public const int SecretRetrievalError = BaseEventId + 4;
        public const int AttemptingToGetAllSecrets = BaseEventId + 5;
        public const int AllSecretsRetrieved = BaseEventId + 6;
        public const int SecretPathOrNoData = BaseEventId + 7;

    [LoggerMessage(EventId = AttemptingToGetSecret, Level = LogLevel.Debug, Message = "HashiCorpVaultSecretManager: Attempting to get secret. Path: '{Path}', Key: '{Key}'.")]
    public static partial void LogAttemptingToGetSecret(ILogger logger, string path, string key);

    [LoggerMessage(EventId = SecretRetrieved, Level = LogLevel.Debug, Message = "HashiCorpVaultSecretManager: Secret retrieved successfully. Path: '{Path}', Key: '{Key}'.")]
    public static partial void LogSecretRetrieved(ILogger logger, string path, string key);

    [LoggerMessage(EventId = SecretOrKeyNotFound, Level = LogLevel.Warning, Message = "HashiCorpVaultSecretManager: Secret data or specific key not found. Path: '{Path}', Key: '{Key}'.")]
    public static partial void LogSecretOrKeyNotFound(ILogger logger, string path, string key);

    [LoggerMessage(EventId = SecretPathNotFound, Level = LogLevel.Warning, Message = "HashiCorpVaultSecretManager: Secret path not found in Vault: {Path}.")]
    public static partial void LogSecretPathNotFound(ILogger logger, string path, Exception ex);

    [LoggerMessage(EventId = SecretRetrievalError, Level = LogLevel.Error, Message = "HashiCorpVaultSecretManager: Error retrieving secret. Path: '{Path}', Key: '{Key}'. Code: {ErrorCode}, Error: {ErrorMessage}.")]
    public static partial void LogSecretRetrievalError(ILogger logger, string path, string key, string errorCode, string? errorMessage, Exception ex);

    [LoggerMessage(EventId = AttemptingToGetAllSecrets, Level = LogLevel.Debug, Message = "HashiCorpVaultSecretManager: Attempting to get all secrets at path: '{Path}'.")]
    public static partial void LogAttemptingToGetAllSecrets(ILogger logger, string path);

    [LoggerMessage(EventId = AllSecretsRetrieved, Level = LogLevel.Debug, Message = "HashiCorpVaultSecretManager: Retrieved {Count} secrets from path: '{Path}'.")]
    public static partial void LogAllSecretsRetrieved(ILogger logger, string path, int count);

    [LoggerMessage(EventId = SecretPathOrNoData, Level = LogLevel.Warning, Message = "HashiCorpVaultSecretManager: Secret path '{Path}' not found or contains no data.")]
    public static partial void LogSecretPathOrNoData(ILogger logger, string path);
}