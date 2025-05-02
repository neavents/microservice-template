using System;
using Microsoft.Extensions.DependencyInjection;

namespace TemporaryName.Infrastructure.ChangeDataCapture.Debezium;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureChangeDataCaptureDebezium(this IServiceCollection services){

        return services;
    }
}
