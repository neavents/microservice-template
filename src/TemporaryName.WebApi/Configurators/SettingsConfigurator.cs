using System;

namespace TemporaryName.WebApi.Configurators;

internal static class SettingsConfigurator
{
    public const string MassTransitFileName = "masstransitsettings";
    public const string CachingFileName = "cachingsettings";
    public const string ObservabilityFileName = "observabilitysettings";
    public const string PersistenceFileName = "persistencesettings";
    public const string SecurityFileName = "securitysettings";

    public static WebApplicationBuilder LoadAndConfigureSettings(this WebApplicationBuilder builder)
    {
        AddJsonFile(builder, MassTransitFileName, envDepended: true);
        AddJsonFile(builder, CachingFileName, envDepended: true);
        AddJsonFile(builder, ObservabilityFileName, envDepended: true);
        AddJsonFile(builder, PersistenceFileName, envDepended: true);
        AddJsonFile(builder, SecurityFileName, envDepended: true);

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
