namespace SharedKernel.Primitives;

public enum ErrorType
{
    None = 0,
    Validation = 1, // Input/rule validation error
    NotFound = 2,   // Resource not found
    Conflict = 3,   // Resource state conflict (e.g., already exists)
    Failure = 4,    // General processing failure
    Unauthorized = 5, // AuthN failed
    Forbidden = 6    // AuthZ failed
}
