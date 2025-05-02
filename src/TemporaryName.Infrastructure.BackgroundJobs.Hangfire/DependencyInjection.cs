using System;
using Microsoft.Extensions.DependencyInjection;

namespace TemporaryName.Infrastructure.BackgroundJobs.Hangfire;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureBackgroundJobsHangfire(this IServiceCollection services){

        return services;
    }
}
