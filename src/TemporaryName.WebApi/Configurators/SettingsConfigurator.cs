using System;

namespace TemporaryName.WebApi.Configurators;

internal static class SettingsConfigurator
{
    public const string MassTransitFileName = "masstransitsettings";

    public static WebApplicationBuilder LoadAndConfigureSettings(this WebApplicationBuilder builder)
    {
        AddJsonFile(builder, MassTransitFileName, envDepended: true);

        return builder;
    }

    private static void AddJsonFile(WebApplicationBuilder builder, string fileName, bool envDepended = false)
    {
        builder.Configuration.AddJsonFile(
        path: $"{fileName}.json",
        optional: true,
        reloadOnChange: true
        );

        if (envDepended)
        {
            AddJsonFile(builder, $"{fileName}.{builder.Environment.EnvironmentName}");
        }
    }

}
