

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using TemporaryName.Infrastructure.HttpClient.Handlers;

namespace TemporaryName.Infrastructure.HttpClient;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureHttpClient(this IServiceCollection services)
    {
        services.AddTransient<CorrelationIdPropagationHandler>();

        services.AddHttpContextAccessor();

        //CorrelationId handler automatically added to all httpclients :D
        services.ConfigureAll<HttpClientFactoryOptions>(opts =>
        {
            opts.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                builder.AdditionalHandlers.Add(builder.Services.GetRequiredService<CorrelationIdPropagationHandler>());
            });
        });

        return services;
    }
}
