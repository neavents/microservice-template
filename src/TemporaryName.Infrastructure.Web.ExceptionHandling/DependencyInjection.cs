using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Middlewares;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling;

public static class DependencyInjection
{
    public static IServiceCollection AddGlobalExceptionHandlingServices(
        this IServiceCollection services,
        Action<GlobalExceptionHandlingOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        // Configure options
        OptionsBuilder<GlobalExceptionHandlingOptions> optionsBuilder = services.AddOptions<GlobalExceptionHandlingOptions>();
        if (configureOptions != null)
        {
            optionsBuilder.Configure(configureOptions);
        }
        else // Provide default configuration if none is specified by the caller
        {
            optionsBuilder.Configure(options =>
            {
                // Default: Include stack trace only if environment name contains "Development"
                // This requires IHostEnvironment to be available when options are resolved,
                // or the consuming app needs to set it based on its IHostEnvironment.
                // A simpler default is just false, and let the app configure it.
                options.IncludeStackTrace = false;
            });
        }
        
        // The ProblemDetailsFactory and IExceptionProblemDetailsMapper instances
        // are expected to be registered by Autofac via AutofacExceptionHandlingModule.
        // This method primarily sets up the options.

        return services;
    }

    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        return app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
