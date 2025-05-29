using System;
using Microsoft.AspNetCore.Http;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.HttpClient.Handlers;

public class CorrelationIdPropagationHandler : DelegatingHandler
{
    private const string CorrelationIdHeaderName = ObservabilityConstants.CorrelationIdHeaderName;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdPropagationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        HttpContext httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is not null &&
            httpContext.Items.TryGetValue(CorrelationIdHeaderName, out object? corrIdObj) &&
            corrIdObj is not null &&
            corrIdObj is string correlationId &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            if (!request.Headers.Contains(CorrelationIdHeaderName))
            {
                request.Headers.Add(CorrelationIdHeaderName, correlationId);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
