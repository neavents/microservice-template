using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Castle.DynamicProxy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Interceptors;

/// <summary>
/// Intercepts method calls to create audit trails for significant operations.
/// It captures who performed the action, what action was performed, on which entity, and the outcome.
/// </summary>
public partial class AuditingInterceptor : IInterceptor
{
    private readonly ILogger<AuditingInterceptor> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor; // To get user information
    // In a real system, you'd have an IAuditLogRepository or similar to persist audit entries.
    // private readonly IAuditLogRepository _auditLogRepository;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        MaxDepth = 3,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    public AuditingInterceptor(
        ILogger<AuditingInterceptor> logger,
        IHttpContextAccessor httpContextAccessor
        /* IAuditLogRepository auditLogRepository */)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        // _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        LogInterceptorInstantiated(_logger, nameof(AuditingInterceptor));
    }

    public void Intercept(IInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        var method = invocation.MethodInvocationTarget ?? invocation.Method;
        var className = method.DeclaringType?.Name ?? "UnknownClass";
        var methodName = method.Name;

        // Basic audit: capture method call, user, and timestamp.
        // Advanced audit: capture parameters, return value changes (diffing), entity identifiers.
        string userId = GetCurrentUserId() ?? "AnonymousOrSystem";
        string? clientIpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        string? userAgent = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();
        string? correlationId = Activity.Current?.Id ?? _httpContextAccessor.HttpContext?.TraceIdentifier;

        LogAuditAttempt(_logger, className, methodName, userId, clientIpAddress ?? "N/A");

        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        string? exceptionType = null;
        string? exceptionMessage = null;
        object? returnValueForAudit = null;

        try
        {
            invocation.Proceed();

            if (invocation.ReturnValue is Task returnValueTask)
            {
                returnValueTask.ContinueWith(task =>
                {
                    stopwatch.Stop();
                    if (task.IsFaulted && task.Exception != null)
                    {
                        var flatEx = task.Exception.Flatten().InnerException ?? task.Exception;
                        success = false;
                        exceptionType = flatEx.GetType().FullName;
                        exceptionMessage = flatEx.Message;
                        LogAuditFailure(_logger, className, methodName, userId, stopwatch.ElapsedMilliseconds, exceptionType ?? "UnknownException", exceptionMessage ?? "N/A");
                    }
                    else if (task.IsCanceled)
                    {
                        success = false; // Or a specific status for canceled
                        exceptionType = "TaskCanceledException";
                        LogAuditCanceled(_logger, className, methodName, userId, stopwatch.ElapsedMilliseconds);
                    }
                    else
                    {
                        success = true;
                        if (method.ReturnType.IsGenericType) // Task<T>
                        {
                             try
                            {
                                returnValueForAudit = task.GetType().GetProperty("Result")?.GetValue(task);
                            }
                            catch(Exception ex)
                            {
                                LogAuditWarning(_logger, className, methodName, userId, "Failed to retrieve result from async Task for auditing.", ex);
                                returnValueForAudit = "[ErrorRetrievingResult]";
                            }
                        }
                        else
                        {
                            returnValueForAudit = "[AsyncVoidOperationSuccess]";
                        }
                        LogAuditSuccess(_logger, className, methodName, userId, stopwatch.ElapsedMilliseconds);
                    }
                    PersistAuditEntry(invocation, userId, clientIpAddress, userAgent, correlationId, success, stopwatch.Elapsed, returnValueForAudit, exceptionType, exceptionMessage);
                }, TaskScheduler.Default);
            }
            else
            {
                stopwatch.Stop();
                success = true;
                 if (method.ReturnType != typeof(void))
                {
                    returnValueForAudit = invocation.ReturnValue;
                }
                else
                {
                    returnValueForAudit = "[SyncVoidOperationSuccess]";
                }
                LogAuditSuccess(_logger, className, methodName, userId, stopwatch.ElapsedMilliseconds);
                PersistAuditEntry(invocation, userId, clientIpAddress, userAgent, correlationId, success, stopwatch.Elapsed, returnValueForAudit, null, null);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            success = false;
            exceptionType = ex.GetType().FullName;
            exceptionMessage = ex.Message;
            LogAuditFailure(_logger, className, methodName, userId, stopwatch.ElapsedMilliseconds, exceptionType ?? "UnknownException", exceptionMessage ?? "N/A", ex);
            PersistAuditEntry(invocation, userId, clientIpAddress, userAgent, correlationId, success, stopwatch.Elapsed, null, exceptionType, exceptionMessage);
            throw; 
        }
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name;
    }

    private void PersistAuditEntry(
        IInvocation invocation,
        string userId,
        string? ipAddress,
        string? userAgent,
        string? correlationId,
        bool success,
        TimeSpan duration,
        object? returnValue,
        string? exceptionType,
        string? exceptionMessage)
    {
        var method = invocation.MethodInvocationTarget ?? invocation.Method;
        var className = method.DeclaringType?.FullName ?? "UnknownClass";
        var methodName = method.Name;

        string parametersJson;
        try
        {
            var simplifiedArgs = invocation.Arguments.Select(arg =>
                arg == null ? "null" : (arg.GetType().IsPrimitive || arg is string || arg is Guid || arg is DateTime || arg is DateTimeOffset ? arg.ToString() : arg.GetType().Name)
            ).ToArray();
            parametersJson = JsonSerializer.Serialize(simplifiedArgs, _jsonSerializerOptions);
        }
        catch (Exception ex)
        {
            parametersJson = $"[Error serializing parameters: {ex.Message}]";
            LogSerializationError(_logger, nameof(AuditingInterceptor), "audit parameters", $"{className}.{methodName}", ex.Message, ex);
        }
        
        string returnValueJson = "[NotApplicable]";
        if (method.ReturnType != typeof(void) && method.ReturnType != typeof(Task))
        {
            try {
                 returnValueJson = returnValue == null ? "null" : JsonSerializer.Serialize(returnValue, _jsonSerializerOptions);
            }
             catch (Exception ex)
            {
                returnValueJson = $"[Error serializing return value: {ex.Message}]";
                LogSerializationError(_logger, nameof(AuditingInterceptor), "audit return value", $"{className}.{methodName}", ex.Message, ex);
            }
        }


        // Placeholder for actual audit persistence
        // var auditEntry = new YourAuditLogEntry
        // {
        //     Timestamp = DateTimeOffset.UtcNow,
        //     UserId = userId,
        //     Action = $"{className}.{methodName}",
        //     Parameters = parametersJson,
        //     Success = success,
        //     DurationMs = duration.TotalMilliseconds,
        //     ReturnValue = returnValueJson, // Serialize return value carefully
        //     ExceptionType = exceptionType,
        //     ExceptionMessage = exceptionMessage,
        //     ClientIpAddress = ipAddress,
        //     UserAgent = userAgent,
        //     CorrelationId = correlationId,
        //     // EntityType, EntityId would require more context or argument inspection
        // };
        // await _auditLogRepository.AddAsync(auditEntry);

        LogAuditEntryPersisted(_logger, className, methodName, userId, success, duration.TotalMilliseconds, parametersJson.Length, returnValueJson.Length, exceptionType ?? "None");
    }
}
