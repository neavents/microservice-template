using System.Text.Json.Serialization;

namespace SharedKernel.Primitives;

[JsonConverter(typeof(JsonStringEnumConverter))] // Ensures enum is serialized as string
public enum ErrorType
{
    // Values should be ordered logically or by commonality if preferred.
    None = 0,
    /// <summary>
    /// A general failure occurred, often client-preventable if the input or request was invalid.
    /// Typically maps to HTTP 400 (Bad Request) or 422 (Unprocessable Entity).
    /// Example: Input data failed validation rules not caught by basic model validation.
    /// </summary>
    Failure = 4, // Default, consider if another default is more appropriate.

    /// <summary>
    /// A requested resource could not be found.
    /// Typically maps to HTTP 404 (Not Found).
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// Input data failed validation. This is more specific than 'Failure' for validation scenarios.
    /// Typically maps to HTTP 400 (Bad Request) or 422 (Unprocessable Entity).
    /// Often accompanied by detailed field-specific error messages.
    /// </summary>
    Validation = 1,

    /// <summary>
    /// A conflict occurred with the current state of a resource.
    /// Typically maps to HTTP 409 (Conflict).
    /// Example: Attempting to create a resource that already exists, or an optimistic concurrency failure.
    /// </summary>
    Conflict = 3,

    /// <summary>
    /// The request requires user authentication, and it has failed or has not yet been provided.
    /// Typically maps to HTTP 401 (Unauthorized).
    /// </summary>
    Unauthorized = 5,

    /// <summary>
    /// The server understood the request, but is refusing to fulfill it.
    /// The authenticated user does not have the necessary permissions for the resource or action.
    /// Typically maps to HTTP 403 (Forbidden).
    /// </summary>
    Forbidden = 6,

    /// <summary>
    /// An unexpected condition was encountered on the server that prevented it from fulfilling the request.
    /// This usually indicates a bug or an unrecoverable server-side issue.
    /// Typically maps to HTTP 500 (Internal Server Error).
    /// </summary>
    Unexpected = 7,

    /// <summary>
    /// A known problem or issue occurred that doesn't fit other categories, often server-side.
    /// Can map to HTTP 4xx or 5xx depending on the nature of the problem.
    /// Example: A downstream service is unavailable (could be 502, 503), or a specific business process failed.
    /// </summary>
    Problem = 8, // Consider renaming if too generic, or use specific types like 'ExternalServiceFailure'

    /// <summary>
    /// The operation is not supported or not implemented.
    /// Typically maps to HTTP 501 (Not Implemented).
    /// </summary>
    NotSupported = 9,

    /// <summary>
    /// The server is currently unable to handle the request due to a temporary overload or maintenance.
    /// Typically maps to HTTP 503 (Service Unavailable).
    /// </summary>
    ServiceUnavailable = 10,

    /// <summary>
    /// A request to a downstream service timed out.
    /// Typically maps to HTTP 504 (Gateway Timeout).
    /// </summary>
    Timeout = 11,

    /// <summary>
    /// The client has sent too many requests in a given amount of time ("rate limiting").
    /// Typically maps to HTTP 429 (Too Many Requests).
    /// </summary>
    RateLimited = 12
}

