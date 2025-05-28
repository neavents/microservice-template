using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Interceptors;

/// <summary>
/// Intercepts method calls to provide comprehensive logging for entry, exit, duration, and errors.
/// Ensures that logging is performant and handles both synchronous and asynchronous methods gracefully.
/// </summary>
public partial class LoggingInterceptor : IInterceptor
{
    private readonly ILogger<LoggingInterceptor> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        MaxDepth = 5, 
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles 
    };

    public LoggingInterceptor(ILogger<LoggingInterceptor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        LogInterceptorInstantiated(_logger, nameof(LoggingInterceptor));
    }

    public void Intercept(IInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        string GetArgumentValue(object? arg, ParameterInfo paramInfo)
        {
            if (arg is null) return "null";

            try
            {
                return JsonSerializer.Serialize(arg, _jsonSerializerOptions);
            }
            catch (JsonException jsonEx)
            {
                LogSerializationError(_logger, nameof(LoggingInterceptor), "argument", paramInfo.Name ?? "UnknownParam", jsonEx.Message, jsonEx);
                return $"[SerializationError: {jsonEx.Message}]";
            }
            catch (Exception ex)
            {
                LogSerializationError(_logger, nameof(LoggingInterceptor), "argument", paramInfo.Name ?? "UnknownParam", ex.Message, ex);
                return $"[UnexpectedSerializationError: {ex.Message}]";
            }
        }

        var method = invocation.MethodInvocationTarget ?? invocation.Method;
        var className = method.DeclaringType?.Name ?? "UnknownClass";
        var methodName = method.Name;
        var arguments = invocation.Arguments.Select((arg, index) =>
        {
            var paramInfo = method.GetParameters()[index];
            return $"{paramInfo.Name}: {GetArgumentValue(arg, paramInfo)}";
        }).ToArray();

        LogMethodEntry(_logger, className, methodName, arguments);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            invocation.Proceed();

            if (invocation.ReturnValue is Task returnValueTask)
            {
                returnValueTask.ContinueWith(task =>
                {
                    stopwatch.Stop();
                    if (task.IsFaulted && task.Exception is not null)
                    {
                        var flattenedException = task.Exception.Flatten();
                        LogMethodException(_logger, className, methodName, stopwatch.ElapsedMilliseconds, flattenedException.InnerExceptions.Count, flattenedException);
                    }
                    else if (task.IsCanceled)
                    {
                        LogMethodCanceled(_logger, className, methodName, stopwatch.ElapsedMilliseconds);
                    }
                    else
                    {
                        object? resultValue = null;
                        string resultValueString = "[TaskCompleted]";

                        if (method.ReturnType.IsGenericType)
                        {
                            try
                            {
                                resultValue = task.GetType().GetProperty("Result")?.GetValue(task);
                                resultValueString = resultValue == null ? "null" : JsonSerializer.Serialize(resultValue, _jsonSerializerOptions);
                            }
                            catch (JsonException jsonEx)
                            {
                                LogSerializationError(_logger, nameof(LoggingInterceptor), "return value", methodName, jsonEx.Message, jsonEx);
                                resultValueString = $"[ReturnValueSerializationError: {jsonEx.Message}]";
                            }
                            catch (Exception ex)
                            {
                                LogSerializationError(_logger, nameof(LoggingInterceptor), "return value", methodName, ex.Message, ex);
                                resultValueString = $"[UnexpectedReturnValueSerializationError: {ex.Message}]";
                            }
                        }
                        LogMethodExit(_logger, className, methodName, stopwatch.ElapsedMilliseconds, resultValueString);
                    }
                }, TaskScheduler.Default);
            }
            else
            {
                stopwatch.Stop();
                string returnValueString = "[VoidMethod]";
                if (method.ReturnType != typeof(void))
                {
                     try
                    {
                        returnValueString = invocation.ReturnValue == null ? "null" : JsonSerializer.Serialize(invocation.ReturnValue, _jsonSerializerOptions);
                    }
                    catch (JsonException jsonEx)
                    {
                        LogSerializationError(_logger, nameof(LoggingInterceptor), "return value", methodName, jsonEx.Message, jsonEx);
                        returnValueString = $"[ReturnValueSerializationError: {jsonEx.Message}]";
                    }
                     catch (Exception ex)
                    {
                        LogSerializationError(_logger, nameof(LoggingInterceptor), "return value", methodName, ex.Message, ex);
                        returnValueString = $"[UnexpectedReturnValueSerializationError: {ex.Message}]";
                    }
                }
                LogMethodExit(_logger, className, methodName, stopwatch.ElapsedMilliseconds, returnValueString);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogMethodException(_logger, className, methodName, stopwatch.ElapsedMilliseconds, 1, ex);
            throw; 
        }
    }
}
