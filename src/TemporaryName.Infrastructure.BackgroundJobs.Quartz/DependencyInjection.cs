using System;
using Microsoft.Extensions.DependencyInjection;

namespace TemporaryName.Infrastructure.BackgroundJobs.Quartz;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureBackgroundJobsQuartz(this IServiceCollection services){

        return services;
    }
}
