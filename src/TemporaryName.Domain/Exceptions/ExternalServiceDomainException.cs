using System;
using SharedKernel.Primitives;

namespace TemporaryName.Domain.Exceptions;

public class ExternalServiceDomainException : DomainException
{
    public string ServiceName { get; }
    public string? OperationName { get; }

    // Constructor 1: Takes an existing Error object
    public ExternalServiceDomainException(
        string serviceName,
        string? operationName,
        Error initialErrorDetails, // The Error object passed in
        Exception? innerException = null)
        : base(
            // Enrich the passed-in ErrorDetails with service and operation name before passing to base
            EnrichErrorDetails(initialErrorDetails, serviceName, operationName),
            // Construct the base exception message and inner exception
            innerException ?? new Exception($"An error occurred with external service '{serviceName}' during operation '{operationName ?? "unknown"}'. See inner exception for transport details.")
          )
    {
        ServiceName = serviceName;
        OperationName = operationName;
    }

    // Constructor 2: Builds an Error object from scratch
    public ExternalServiceDomainException(
        string serviceName,
        string? operationName,
        string message, // Message for the Error object's description
        ErrorType errorType = ErrorType.Unexpected,
        Dictionary<string, object?>? initialCustomMetadata = null, // User-provided custom metadata
        Exception? innerException = null)
        : base(
            // Create the complete Error object with all necessary metadata before passing to base
            CreateErrorDetailsWithServiceInfo(serviceName, operationName, message, errorType, initialCustomMetadata),
            innerException
          )
    {
        ServiceName = serviceName;
        OperationName = operationName;
    }

    // Helper to enrich an existing Error object for Constructor 1
    private static Error EnrichErrorDetails(Error error, string serviceName, string? operationName)
    {
        Error updatedError = error.WithAddedMetadata("externalServiceName", serviceName);
        if (!string.IsNullOrWhiteSpace(operationName))
        {
            updatedError = updatedError.WithAddedMetadata("externalOperationName", operationName);
        }
        return updatedError;
    }

    // Helper to create a new Error object with service info for Constructor 2
    private static Error CreateErrorDetailsWithServiceInfo(
        string serviceName,
        string? operationName,
        string message,
        ErrorType errorType,
        Dictionary<string, object?>? customMetadata)
    {
        // Start with the custom metadata or a new dictionary
        var metadataBuilder = customMetadata != null
            ? new Dictionary<string, object?>(customMetadata) // Copy to avoid modifying caller's dictionary
            : new Dictionary<string, object?>();

        // Add/overwrite service-specific metadata
        metadataBuilder["externalServiceName"] = serviceName;
        if (!string.IsNullOrWhiteSpace(operationName))
        {
            metadataBuilder["externalOperationName"] = operationName;
        }

        string errorCode = $"ExternalService.{serviceName}.Failure{(string.IsNullOrWhiteSpace(operationName) ? "" : $".{operationName}")}";

        return new Error(errorCode, message, errorType, metadataBuilder); // Error constructor will make metadata IReadOnlyDictionary
    }
}