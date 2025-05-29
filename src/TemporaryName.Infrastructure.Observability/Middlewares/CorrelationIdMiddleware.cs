using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Serilog.Context;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.Observability.Middlewares;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = ObservabilityConstants.CorrelationIdHeaderName;
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string correlationId = GetOrGenerateCorrId(context);
        AddCorrIdToResponse(context, correlationId);
        context.Items[CorrelationIdHeaderName] = correlationId;

        var apmTransaction = Elastic.Apm.Agent.Tracer.CurrentTransaction;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            if (apmTransaction is not null)
            {
                LogContext.PushProperty("ElasticApmTraceId", apmTransaction.TraceId);
                LogContext.PushProperty("ElasticApmTransactionId", apmTransaction.Id);
            }

            await _next(context);
        }
    }

    private static string GetOrGenerateCorrId(HttpContext context)
    {
        StringValues correlationIdValues;
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out correlationIdValues) &&
            correlationIdValues.Count != 0 &&
            !string.IsNullOrWhiteSpace(correlationIdValues.First()))
        {
            return correlationIdValues.First()!;
        }

        return Guid.NewGuid().ToString();
    }

    private static void AddCorrIdToResponse(HttpContext context, string corrId)
    {
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
            {
                context.Response.Headers.Append(CorrelationIdHeaderName, corrId);
            }

            return Task.CompletedTask;
        });
    }
}
