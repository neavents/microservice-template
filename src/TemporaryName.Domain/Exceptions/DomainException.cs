using System;
using SharedKernel.Primitives;

namespace TemporaryName.Domain.Exceptions;


/// <summary>
/// Base class for custom domain-specific exceptions.
/// Encapsulates one or more <see cref="Error"/> objects providing structured error information.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Gets the primary error details associated with this exception.
    /// If multiple errors are present (e.g., in validation), this might be a summary error.
    /// </summary>
    public Error ErrorDetails { get; }

    /// <summary>
    /// Gets a collection of all errors associated with this exception.
    /// For single-error exceptions, this will contain only <see cref="ErrorDetails"/>.
    /// For aggregate exceptions (like validation), this will contain all individual errors.
    /// </summary>
    public IReadOnlyList<Error> Errors { get; }

    protected DomainException(Error error) 
        : base(error.Description ?? error.Code)
    {
        ErrorDetails = error ?? throw new ArgumentNullException(nameof(error));
        Errors = new List<Error> { error }.AsReadOnly();
    }

    protected DomainException(string message, Error error)
        : base(message)
    {
        ErrorDetails = error ?? throw new ArgumentNullException(nameof(error));
        Errors = new List<Error> { error }.AsReadOnly();
    }

    protected DomainException(Error primaryError, IEnumerable<Error> allErrors)
        : base(primaryError.Description ?? primaryError.Code)
    {
        ErrorDetails = primaryError ?? throw new ArgumentNullException(nameof(primaryError));
        Errors = allErrors?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(allErrors));
        if (!Errors.Any())
        {
            throw new ArgumentException("Error collection cannot be empty.", nameof(allErrors));
        }
    }
    
    protected DomainException(string message, Error primaryError, IEnumerable<Error> allErrors)
        : base(message)
    {
        ErrorDetails = primaryError ?? throw new ArgumentNullException(nameof(primaryError));
        Errors = allErrors?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(allErrors));
        if (!Errors.Any())
        {
            throw new ArgumentException("Error collection cannot be empty.", nameof(allErrors));
        }
    }

    protected DomainException(Error error, Exception innerException)
        : base(error.Description ?? error.Code, innerException)
    {
        ErrorDetails = error ?? throw new ArgumentNullException(nameof(error));
        Errors = new List<Error> { error }.AsReadOnly();
    }
     protected DomainException(string message, Error error, Exception innerException)
        : base(message, innerException)
    {
        ErrorDetails = error ?? throw new ArgumentNullException(nameof(error));
        Errors = new List<Error> { error }.AsReadOnly();
    }


}
