using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace SharedKernel.Primitives; // Adjust namespace as needed

public class Result
{
    private static readonly Result Ok = new(true, Error.None);
    private readonly List<Error>? _errors;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error PrimaryError { get; } // First or most significant error
    public IReadOnlyCollection<Error> Errors => _errors?.AsReadOnly() ?? (PrimaryError == Error.None ? [] : [PrimaryError]);

    protected Result(bool isSuccess, Error error)
    {
        if ((isSuccess && error != Error.None) || (!isSuccess && error == Error.None))
            throw new ArgumentException("Invalid error state for result status.", nameof(error));

        IsSuccess = isSuccess;
        PrimaryError = error;
        if (!isSuccess) _errors = [error];
    }

    protected Result(bool isSuccess, List<Error> errors)
    {
        if (isSuccess && errors.Count != 0)
            throw new ArgumentException("Successful result cannot contain errors.", nameof(errors));
        if (!isSuccess && errors.Count == 0)
            throw new ArgumentException("Failed result must contain at least one error.", nameof(errors));

        IsSuccess = isSuccess;
        PrimaryError = errors.FirstOrDefault() ?? Error.None;
        _errors = isSuccess ? null : errors;
    }

    public static Result Success() => Ok;
    public static Result Failure(Error error) => new(false, error);
    public static Result Failure(IEnumerable<Error> errors)
    {
        List<Error> distinctErrors = errors?.Where(e => e != Error.None).Distinct().ToList() ?? [];
        if (distinctErrors.Count == 0)
            throw new ArgumentException("Must provide at least one valid error.", nameof(errors));
        return new(false, distinctErrors);
    }

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
    public static Result<TValue> Failure<TValue>(IEnumerable<Error> errors) => new(default, false, errors);

    public static Result Combine(params Result[] results)
    {
        ArgumentNullException.ThrowIfNull(results);
        List<Error> errors = results.Where(r => r.IsFailure).SelectMany(r => r.Errors).Distinct().ToList();
        return errors.Count > 0 ? Failure(errors) : Success();
    }

    public static async Task<Result> CombineAsync(params Task<Result>[] tasks)
    {
        Result[] results = await Task.WhenAll(tasks);
        return Combine(results);
    }

    // --- Functional Extensions ---

    public T Match<T>(Func<T> onSuccess, Func<IReadOnlyCollection<Error>, T> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Errors);

    public Result Tap(Action onSuccess)
    {
        if (IsSuccess) onSuccess();
        return this;
    }

    public async Task<Result> TapAsync(Func<Task> onSuccess)
    {
        if (IsSuccess) await onSuccess().ConfigureAwait(false);
        return this;
    }

    public Result OnFailure(Action<IReadOnlyCollection<Error>> onFailure)
    {
        if (IsFailure) onFailure(Errors);
        return this;
    }

     public Result Check(Func<Result> func) =>
        IsSuccess ? func() : this;

    public async Task<Result> CheckAsync(Func<Task<Result>> func) =>
        IsSuccess ? await func().ConfigureAwait(false) : this;
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    [MaybeNull]
    public TValue ValueOrDefault => _value; // Allows access even on failure, returns default

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access {nameof(Value)} of a failed {nameof(Result)}<{typeof(TValue).Name}>. Error(s): {string.Join(", ", Errors.Select(e => e.Code))}");

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    protected internal Result(TValue? value, bool isSuccess, IEnumerable<Error> errors)
        : base(isSuccess, errors?.ToList() ?? [])
    {
        _value = value;
    }

    public static implicit operator Result<TValue>(TValue value) => Success<TValue>(value);

    // --- Functional Extensions ---

    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<IReadOnlyCollection<Error>, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Errors);

    public Result<TValue> Tap(Action<TValue> onSuccess)
    {
        if (IsSuccess) onSuccess(Value);
        return this;
    }

     public async Task<Result<TValue>> TapAsync(Func<TValue, Task> onSuccess)
    {
        if (IsSuccess) await onSuccess(Value).ConfigureAwait(false);
        return this;
    }

    // Inherits OnFailure from base Result

    public Result<TNextValue> Map<TNextValue>(Func<TValue, TNextValue> mappingFunc) =>
        IsSuccess ? Result.Success(mappingFunc(Value)) : Result.Failure<TNextValue>(Errors);

    public async Task<Result<TNextValue>> MapAsync<TNextValue>(Func<TValue, Task<TNextValue>> mappingFunc) =>
        IsSuccess ? Result.Success(await mappingFunc(Value).ConfigureAwait(false)) : Result.Failure<TNextValue>(Errors);

    public Result<TNextValue> Bind<TNextValue>(Func<TValue, Result<TNextValue>> func) =>
        IsSuccess ? func(Value) : Result.Failure<TNextValue>(Errors);

    public async Task<Result<TNextValue>> BindAsync<TNextValue>(Func<TValue, Task<Result<TNextValue>>> func) =>
        IsSuccess ? await func(Value).ConfigureAwait(false) : Result.Failure<TNextValue>(Errors);

    public Result Bind(Func<TValue, Result> func) =>
        IsSuccess ? func(Value) : Result.Failure(Errors);

    public async Task<Result> BindAsync(Func<TValue, Task<Result>> func) =>
        IsSuccess ? await func(Value).ConfigureAwait(false) : Result.Failure(Errors);

     public Result<TValue> Check(Func<TValue, Result> func) =>
        IsSuccess ? func(Value).IsSuccess ? this : Result.Failure<TValue>(func(Value).Errors) : this;
        // This ^ is simplified; a real Check would accumulate errors if func() fails

    public async Task<Result<TValue>> CheckAsync(Func<TValue, Task<Result>> func)
    {
        if (IsFailure) return this;
        Result checkResult = await func(Value).ConfigureAwait(false);
        return checkResult.IsSuccess ? this : Result.Failure<TValue>(checkResult.Errors);
    }
}