using System;
using System.Globalization;
using TemporaryName.Domain.Resources;

namespace TemporaryName.WebApi.Configurators;

public static class LocalizationConfigurator
{
    private const string defaultCulture = "en-US";
    private static readonly CultureInfo[] supportedCultures = [
            new(defaultCulture),
            new("tr-TR")
        ];

    public static IServiceCollection ConfigureLocalication(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");

        return services;
    }

    public static IMvcBuilder ConfigureDataAnnotationsLocalication(this IMvcBuilder mvcBuilder)
    {
        mvcBuilder.AddDataAnnotationsLocalization(options =>
        {
            options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(typeof(ValidationMessages));
        });

        return mvcBuilder;
    }

    public static WebApplication ConfigureRequestLocalization(this WebApplication app)
    {
        app.UseRequestLocalization(
            new RequestLocalizationOptions
            {
                ApplyCurrentCultureToResponseHeaders = true,
                DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            }
        );

        return app;
    }
}
