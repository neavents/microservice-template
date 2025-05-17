using System;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Services;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Middlewares;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly GlobalExceptionHandlingOptions _options;
    private readonly IHostEnvironment _environment; // To check for Development environment

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false // More compact for production; could be configurable
    };

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        ProblemDetailsFactory problemDetailsFactory,
        IOptions<GlobalExceptionHandlingOptions> options,
        IHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _problemDetailsFactory = problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));

        if (_environment.IsDevelopment() && _options.IncludeStackTrace) {
            _serializerOptions.WriteIndented = true; // Prettier JSON in dev if stack traces are on
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        try
        {
            await _next(context);
        }
        // It's generally better to catch specific, anticipated exceptions higher up if they can be handled gracefully.
        // This middleware is for unhandled exceptions or those explicitly bubbled up.
        catch (Exception ex)
        {
            // Check if the response has already started. If so, we can't write a new response.
            // This is an edge case, e.g., an error during streaming.
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(ex, "Response has already started, cannot write ProblemDetails for unhandled exception. Path: {RequestPath}", context.Request.Path);
                throw; // Re-throw the exception, as we can't modify the response.
            }

            string correlationId = _options.GetCorrelationId?.Invoke(context)
                                   ?? Activity.Current?.Id
                                   ?? context.TraceIdentifier;

            // Log the full exception details, regardless of environment.
            // Consider using structured logging properties for better querying.
            _logger.LogError(ex, "Unhandled exception caught by GlobalExceptionHandlingMiddleware. CorrelationId: {CorrelationId}, Request: {RequestMethod} {RequestPath}",
                correlationId,
                context.Request.Method,
                context.Request.Path);

            context.Response.Clear();

            ProblemDetails problemDetails = _problemDetailsFactory.Create(context, ex);

            if (!problemDetails.Extensions.ContainsKey(ProblemDetailsConstants.TraceIdExtensionKey))
            {
                problemDetails.Extensions[ProblemDetailsConstants.TraceIdExtensionKey] = correlationId;
            }
            
            context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            try
            {
                await JsonSerializer.SerializeAsync(context.Response.Body, problemDetails, _serializerOptions).ConfigureAwait(false);
            }
            catch (Exception serializationEx)
            {
                // If serialization itself fails, log it and try to return a very basic error.
                _logger.LogCritical(serializationEx, "Failed to serialize ProblemDetails. CorrelationId: {CorrelationId}", correlationId);
                // Attempt to write a plain text error if JSON serialization fails
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("An unexpected error occurred, and the error details could not be serialized.").ConfigureAwait(false);
            }
        }
    }
}
