using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Services;

public class ProblemDetailsFactory
{
    private readonly IEnumerable<IExceptionProblemDetailsMapper> _mappers;
    private readonly GlobalExceptionHandlingOptions _options;
    private readonly ILogger<ProblemDetailsFactory> _logger;

    public ProblemDetailsFactory(
        IEnumerable<IExceptionProblemDetailsMapper> mappers,
        IOptions<GlobalExceptionHandlingOptions> options,
        ILogger<ProblemDetailsFactory> logger)
    {
        _mappers = mappers
            .OrderBy(m => m.Order)
            .ThenByDescending(m => ProblemDetailsHelpers.GetInheritanceDepth(m.HandledExceptionType))
            .ToList();

        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (!_mappers.Any())
        {
            _logger.LogWarning("No {MapperInterfaceName} instances found. ProblemDetails generation might be limited.", nameof(IExceptionProblemDetailsMapper));
        }
        else
        {
            _logger.LogDebug("Initialized {FactoryName} with {MapperCount} mappers: {MapperTypes}",
                nameof(ProblemDetailsFactory),
                _mappers.Count(),
                string.Join(", ", _mappers.Select(m => m.GetType().Name)));
        }
    }

    public ProblemDetails Create(HttpContext httpContext, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(httpContext, nameof(httpContext));
        ArgumentNullException.ThrowIfNull(exception, nameof(exception));

        Type exceptionType = exception.GetType();

        // First, check for direct custom mappings in options (for simple overrides)
        if (_options.CustomProblemDetailsMappings.TryGetValue(exceptionType.FullName ?? exceptionType.Name, out Func<HttpContext, Exception, GlobalExceptionHandlingOptions, ProblemDetails>? customMappingFunc))
        {
            _logger.LogDebug("Using direct custom mapping from options for exception type {ExceptionType}.", exceptionType.FullName);
            return customMappingFunc(httpContext, exception, _options);
        }

        // Find the best mapper
        IExceptionProblemDetailsMapper? mapper = null;
        foreach (IExceptionProblemDetailsMapper currentMapper in _mappers)
        {
            if (currentMapper.HandledExceptionType.IsAssignableFrom(exceptionType))
            {
                mapper = currentMapper;
                break;
            }
        }

        if (mapper == null)
        {
            _logger.LogError("No suitable {MapperInterfaceName} found for exception type {ExceptionType}. This should not happen if a default mapper for System.Exception is registered.",
                nameof(IExceptionProblemDetailsMapper), exceptionType.FullName);
            // Fallback to a very basic ProblemDetails if no mapper is found (should be rare if DefaultExceptionMapper is present)
            return new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "No specific error mapper was found for this exception type.",
                Type = ProblemDetailsHelpers.CombineProblemTypeUri(_options.ProblemTypeUriBase, "unmapped-error")
            };
        }

        _logger.LogDebug("Using mapper {MapperType} for exception type {ExceptionType}.", mapper.GetType().FullName, exceptionType.FullName);
        return mapper.CreateProblemDetails(httpContext, exception, _options);
    }


}
