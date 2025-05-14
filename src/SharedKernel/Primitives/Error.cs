using System;

namespace SharedKernel.Primitives;

public record Error : IEquatable<Error>
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.", ErrorType.Validation);

    public string Code { get; }
    public string Description { get; }
    public ErrorType Type { get; }

    public Error(string code, string description, ErrorType type)
    {
        // Basic validation, could add more checks for empty strings etc.
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(description);

        Code = code;
        Description = description;
        Type = type;
    }

    public virtual bool Equals(Error? other) => other is not null && Code == other.Code && Type == other.Type;
    public override int GetHashCode() => HashCode.Combine(Code, Type);

}