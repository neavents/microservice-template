
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TemporaryName.Infrastructure.Outbox.EFCore;

public static class DependencyInjection
{
    public static IServiceCollection AddOutboxInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        
        return services;
    }
}