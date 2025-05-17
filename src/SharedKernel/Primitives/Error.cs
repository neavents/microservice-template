using System;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace SharedKernel.Primitives;
public record Error
{
    public string Code { get; init; }
    public string? Description { get; init; }
    public ErrorType Type { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }

    public Error(
        string code,
        string? description = null,
        ErrorType type = ErrorType.Failure,
        IReadOnlyDictionary<string, object?>? initialMetadata = null)
    {

        bool isNoneBeingConstructed = string.IsNullOrEmpty(code) &&
                                    string.IsNullOrEmpty(description) &&
                                    type == ErrorType.Failure &&
                                    initialMetadata == null;

        if (!isNoneBeingConstructed)
        {
            if (code is null)
            {
                throw new ArgumentNullException(nameof(code), "Error code parameter cannot be null.");
            }
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Error code parameter cannot be empty or whitespace for non-'None' errors.", nameof(code));
            }
        }

        this.Code = code;
        this.Description = description;
        this.Type = type;

        this.Metadata = initialMetadata switch
        {
            null => null,
            ReadOnlyDictionary<string, object?> rod => rod,
            IDictionary<string, object?> dict => new ReadOnlyDictionary<string, object?>(dict),
            _ => throw new ArgumentException("InitialMetadata parameter must be an IDictionary<string, object?> or null to be convertible to IReadOnlyDictionary<string, object?>.", nameof(initialMetadata))
        };
    }

    /// <summary>
    /// Creates a new Error instance with additional metadata.
    /// If the key already exists, its value will be updated.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>A new Error instance with the added or updated metadata.</returns>
    public Error WithAddedMetadata(string key, object? value)
    {
        // Start with existing metadata, or an empty dictionary if Metadata is null
        Dictionary<string, object?> newMetadataDict = this.Metadata != null
            ? new Dictionary<string, object?>(this.Metadata) 
            : [];

        newMetadataDict[key] = value;

        return this with { Metadata = new ReadOnlyDictionary<string, object?>(newMetadataDict) };
    }

    /// <summary>
    /// Creates a new Error instance with a completely new set of metadata.
    /// </summary>
    /// <param name="newMetadata">The complete new set of metadata. Can be null.</param>
    /// <returns>A new Error instance with the specified metadata.</returns>
    public Error WithMetadataSet(IReadOnlyDictionary<string, object?>? newCompleteMetadata)
    {
        IReadOnlyDictionary<string, object?>? finalMetadataToSet = null;
        if (newCompleteMetadata != null)
        {
            if (newCompleteMetadata is ReadOnlyDictionary<string, object?> rod)
            {
                finalMetadataToSet = rod;
            }
            else
            {
                finalMetadataToSet = new ReadOnlyDictionary<string, object?>(
                    new Dictionary<string, object?>(newCompleteMetadata)
                );
            }
        }
        return this with { Metadata = finalMetadataToSet };
    }

    /// <summary>
    /// Represents no error. This should be used cautiously.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure, null);

    /// <summary>
    /// Represents an error due to an unexpected null value.
    /// </summary>
    public static readonly Error NullValue = new("General.NullValue", "A required value was unexpectedly null.", ErrorType.Unexpected, null);

    public static Error Validation(string code, string description, IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, description, ErrorType.Validation, metadata);

    public static Error NotFound(string code, string description, IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, description, ErrorType.NotFound, metadata);

    public static Error Conflict(string code, string description, IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, description, ErrorType.Conflict, metadata);

    public static Error Unauthorized(string code, string description, IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, description, ErrorType.Unauthorized, metadata);

    public static Error Forbidden(string code, string description, IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, description, ErrorType.Forbidden, metadata);

    public static Error Unexpected(string code, string description, IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, description, ErrorType.Unexpected, metadata);

    public static Error Failure(string code, string description, IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, description, ErrorType.Failure, metadata);

    public static Error Problem(string code, string description, IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, description, ErrorType.Problem, metadata);

    /// <summary>
    /// Returns a string representation of the error, primarily for debugging.
    /// </summary>
    public override string ToString()
    {
        string descOutput = string.IsNullOrWhiteSpace(Description) ? "No description provided" : Description;
        string typeOutput = Type.ToString();
        string metaOutput = string.Empty;

        if (Metadata != null && Metadata.Any())
        {
            metaOutput = " | Metadata: " + string.Join(", ", Metadata.Select(kvp => $"{kvp.Key}={(kvp.Value ?? "null")}"));
        }

        return $"Error [{Code}]: {descOutput} (Type: {typeOutput}){metaOutput}";
    }
}